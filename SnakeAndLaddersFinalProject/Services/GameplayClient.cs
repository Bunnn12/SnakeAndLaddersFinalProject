using System;
using System.ServiceModel;
using System.Threading.Tasks;
using SnakeAndLaddersFinalProject.GameplayService;

namespace SnakeAndLaddersFinalProject.Services
{
    public sealed class GameplayClient : IGameplayClient
    {
        private const string ENDPOINT_NAME = "NetTcpBinding_IGameplayService";

        public async Task<RollDiceResponseDto> GetRollDiceAsync(int gameId, int playerUserId)
        {
            var client = new GameplayServiceClient(ENDPOINT_NAME);

            try
            {
                var request = new RollDiceRequestDto
                {
                    GameId = gameId,
                    PlayerUserId = playerUserId
                };

                return await client.RollDiceAsync(request).ConfigureAwait(false);
            }
            finally
            {
                CloseSafely(client);
            }
        }

        public async Task<GetGameStateResponseDto> GetGameStateAsync(int gameId)
        {
            var client = new GameplayServiceClient(ENDPOINT_NAME);

            try
            {
                var request = new GetGameStateRequestDto
                {
                    GameId = gameId
                };

                return await client.GetGameStateAsync(request).ConfigureAwait(false);
            }
            finally
            {
                CloseSafely(client);
            }
        }

        private static void CloseSafely(ICommunicationObject client)
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
