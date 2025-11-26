using System.Collections.Generic;
using System.Threading.Tasks;
using SnakeAndLaddersFinalProject.LobbyService;

namespace SnakeAndLaddersFinalProject.Infrastructure
{
    public interface ILobbyEventsHandler
    {
        Task HandleLobbyUpdatedAsync(LobbyInfo lobby);

        Task HandleLobbyClosedAsync(int partidaId, string reason);

        Task HandleKickedFromLobbyAsync(int partidaId, string reason);

        Task HandlePublicLobbiesChangedAsync(IList<LobbySummary> lobbies);
    }
}
