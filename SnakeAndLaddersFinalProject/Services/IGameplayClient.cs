using System;
using System.Threading.Tasks;
using SnakeAndLaddersFinalProject.GameplayService;

namespace SnakeAndLaddersFinalProject.Services
{
    public interface IGameplayClient : IDisposable
    {
        Task<RollDiceResponseDto> RollDiceAsync(int gameId, int playerUserId);
    }
}
