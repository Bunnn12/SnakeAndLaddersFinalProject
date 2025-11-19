using System.Threading.Tasks;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.GameBoardService;

namespace SnakeAndLaddersFinalProject.Services
{
    public interface IGameplayClient
    {
        Task<RollDiceResponseDto> GetRollDiceAsync(int gameId, int playerUserId);

        Task<GetGameStateResponseDto> GetGameStateAsync(int gameId);
    }
}
