using System.Threading.Tasks;
using SnakeAndLaddersFinalProject.GameplayService;

namespace SnakeAndLaddersFinalProject.Services
{
    public interface IGameplayClient
    {
        Task JoinGameAsync(int gameId, int userId, string userName);

        Task LeaveGameAsync(int gameId, int userId, string reason);

        Task<RollDiceResponseDto> GetRollDiceAsync(int gameId, int playerUserId);

        Task<GetGameStateResponseDto> GetGameStateAsync(int gameId);

        Task<UseItemResponseDto> UseItemAsync(
        int gameId,
        int userId,
        byte itemSlotNumber,
        int? targetUserId);
    }
}
