using System;
using System.ServiceModel;
using System.Threading.Tasks;
using SnakeAndLaddersFinalProject.GameplayService;

namespace SnakeAndLaddersFinalProject.Services
{
    public sealed class GameplayClient : IGameplayClient
    {
        private const string GAMEPLAY_ENDPOINT = "NetTcpBinding_IGameplayService";

        private readonly GameplayServiceClient client;

        public GameplayClient()
        {
            client = new GameplayServiceClient(GAMEPLAY_ENDPOINT);
        }

        public async Task<RollDiceResponseDto> RollDiceAsync(int gameId, int playerUserId)
        {
            var request = new RollDiceRequestDto
            {
                GameId = gameId,
                PlayerUserId = playerUserId
            };

            return await client.RollDiceAsync(request).ConfigureAwait(false);
        }

        public void Dispose()
        {
            try
            {
                if (client.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }
            catch
            {
                client.Abort();
            }
        }
    }
}
