using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Services
{
    internal sealed class GameplayClient : IGameplayClient, IDisposable
    {
        private readonly IGameplayService _gameplayProxy;
        private readonly DuplexChannelFactory<IGameplayService> _channelFactory;

        public GameplayClient(IGameplayEventsHandler eventsHandler)
        {
            if (eventsHandler == null)
            {
                throw new ArgumentNullException(nameof(eventsHandler));
            }

            var callback = new GameplayClientCallback(eventsHandler);
            var instanceContext = new InstanceContext(callback);

            _channelFactory = new DuplexChannelFactory<IGameplayService>(
                instanceContext,
                "NetTcpBinding_IGameplayService");

            _gameplayProxy = _channelFactory.CreateChannel();
        }

        public Task<RollDiceResponseDto> GetRollDiceAsync(
            int gameId,
            int userId,
            byte? diceSlotNumber)
        {
            return Task.Run(
                () =>
                {
                    var request = new RollDiceRequestDto
                    {
                        GameId = gameId,
                        PlayerUserId = userId,
                        DiceSlotNumber = diceSlotNumber
                    };

                    return _gameplayProxy.RollDice(request);
                });
        }

        public Task RegisterTurnTimeoutAsync(int gameId, int userId)
        {
            
            return Task.Run(
                () =>
                {
                    _gameplayProxy.RegisterTurnTimeout(gameId);
                });
        }


        public Task<GetGameStateResponseDto> GetGameStateAsync(int gameId)
        {
            return Task.Run(
                () =>
                {
                    var request = new GetGameStateRequestDto
                    {
                        GameId = gameId
                    };

                    return _gameplayProxy.GetGameState(request);
                });
        }

        public Task JoinGameAsync(int gameId, int userId, string userName)
        {
            return Task.Run(
                () =>
                {
                    _gameplayProxy.JoinGame(gameId, userId, userName);
                });
        }

        public Task LeaveGameAsync(int gameId, int userId, string reason)
        {
            return Task.Run(
                () =>
                {
                    _gameplayProxy.LeaveGame(gameId, userId, reason);
                });
        }

        public Task<UseItemResponseDto> UseItemAsync(
            int gameId,
            int userId,
            byte itemSlotNumber,
            int? targetUserId)
        {
            var request = new UseItemRequestDto
            {
                GameId = gameId,
                PlayerUserId = userId,
                ItemSlotNumber = itemSlotNumber,
                TargetUserId = targetUserId
            };

            return _gameplayProxy.UseItemAsync(request);
        }

        public void Dispose()
        {
            try
            {
                if (_gameplayProxy is ICommunicationObject communicationObject)
                {
                    if (communicationObject.State == CommunicationState.Faulted)
                    {
                        communicationObject.Abort();
                    }
                    else
                    {
                        communicationObject.Close();
                    }
                }
            }
            catch
            {
                if (_gameplayProxy is ICommunicationObject communicationObject)
                {
                    communicationObject.Abort();
                }
            }

            try
            {
                if (_channelFactory != null)
                {
                    if (_channelFactory.State == CommunicationState.Faulted)
                    {
                        _channelFactory.Abort();
                    }
                    else
                    {
                        _channelFactory.Close();
                    }
                }
            }
            catch
            {
                if (_channelFactory != null)
                {
                    _channelFactory.Abort();
                }
            }
        }
    }
}
