using SnakeAndLaddersFinalProject.Game;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Services
{
    internal sealed class GameplayClient : IGameplayClient, IDisposable
    {
        private readonly IGameplayService gameplayProxy;
        private readonly DuplexChannelFactory<IGameplayService> channelFactory;

        public GameplayClient(IGameplayEventsHandler eventsHandler)
        {
            if (eventsHandler == null)
            {
                throw new ArgumentNullException(nameof(eventsHandler));
            }

            var callback = new GameplayClientCallback(eventsHandler);
            var instanceContext = new InstanceContext(callback);

            // Usa el endpoint duplex definido en App.config
            // <endpoint name="NetTcpBinding_IGameplayService" ... />
            channelFactory = new DuplexChannelFactory<IGameplayService>(
                instanceContext,
                "NetTcpBinding_IGameplayService");

            gameplayProxy = channelFactory.CreateChannel();
        }

        public Task<RollDiceResponseDto> GetRollDiceAsync(int gameId, int userId)
        {
            return Task.Run(
                () =>
                {
                    var request = new RollDiceRequestDto
                    {
                        GameId = gameId,
                        PlayerUserId = userId
                    };

                    return gameplayProxy.RollDice(request);
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

                    return gameplayProxy.GetGameState(request);
                });
        }

        public Task JoinGameAsync(int gameId, int userId, string userName)
        {
            return Task.Run(
                () =>
                {
                    gameplayProxy.JoinGame(gameId, userId, userName);
                });
        }

        public Task LeaveGameAsync(int gameId, int userId, string reason)
        {
            return Task.Run(
                () =>
                {
                    gameplayProxy.LeaveGame(gameId, userId, reason);
                });
        }

        public void Dispose()
        {
            try
            {
                if (gameplayProxy is ICommunicationObject communicationObject)
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
                if (gameplayProxy is ICommunicationObject communicationObject)
                {
                    communicationObject.Abort();
                }
            }

            try
            {
                if (channelFactory != null)
                {
                    if (channelFactory.State == CommunicationState.Faulted)
                    {
                        channelFactory.Abort();
                    }
                    else
                    {
                        channelFactory.Close();
                    }
                }
            }
            catch
            {
                if (channelFactory != null)
                {
                    channelFactory.Abort();
                }
            }
        }
    }
}
