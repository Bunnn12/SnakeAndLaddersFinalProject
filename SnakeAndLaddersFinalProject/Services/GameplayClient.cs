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
            int playerUserId,
            byte? diceSlotNumber)
        {
            return Task.Run(
                () =>
                {
                    var request = new RollDiceRequestDto
                    {
                        GameId = gameId,
                        PlayerUserId = playerUserId,
                        DiceSlotNumber = diceSlotNumber
                    };

                    return _gameplayProxy.RollDice(request);
                });
        }

        public Task RegisterTurnTimeoutAsync(int gameId, int playerUserId)
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

        public Task JoinGameAsync(int gameId, int playerUserId, string userName)
        {
            return Task.Run(
                () =>
                {
                    _gameplayProxy.JoinGame(gameId, playerUserId, userName);
                });
        }

        public Task LeaveGameAsync(int gameId, int playerUserId, string reason)
        {
            return Task.Run(
                () =>
                {
                    _gameplayProxy.LeaveGame(gameId, playerUserId, reason);
                });
        }

        public Task<UseItemResponseDto> UseItemAsync(
            int gameId,
            int playerUserId,
            byte itemSlotNumber,
            int? targetUserId)
        {
            var request = new UseItemRequestDto
            {
                GameId = gameId,
                PlayerUserId = playerUserId,
                ItemSlotNumber = itemSlotNumber,
                TargetUserId = targetUserId
            };

            return _gameplayProxy.UseItemAsync(request);
        }

        public void Dispose()
        {
            DisposeCommunicationObject(_gameplayProxy as ICommunicationObject);
            DisposeCommunicationObject(_channelFactory);
        }

        private static void DisposeCommunicationObject(ICommunicationObject communicationObject)
        {
            if (communicationObject == null)
            {
                return;
            }

            try
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
            catch
            {
                communicationObject.Abort();
            }
        }
    }
}
