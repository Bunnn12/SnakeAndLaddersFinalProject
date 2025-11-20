using System.Threading.Tasks;
using SnakeAndLaddersFinalProject.GameplayService;

namespace SnakeAndLaddersFinalProject.Infrastructure
{
    public interface IGameplayEventsHandler
    {
        Task HandleServerPlayerMovedAsync(PlayerMoveResultDto move);

        Task HandleServerTurnChangedAsync(TurnChangedDto turnInfo);

        Task HandleServerPlayerLeftAsync(PlayerLeftDto playerLeftInfo);
    }
}
