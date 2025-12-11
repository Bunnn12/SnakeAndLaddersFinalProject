using System.Collections.Generic;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.Services
{
    internal static class LobbyMembershipService
    {
        public static bool IsCurrentUserHost(
            int currentUserId,
            string currentUserName,
            int hostUserId,
            string hostUserName,
            IEnumerable<LobbyMemberViewModel> lobbyMembers)
        {
            foreach (var lobbyMember in lobbyMembers)
            {
                if (lobbyMember == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(lobbyMember.UserName) &&
                    string.Equals(lobbyMember.UserName, currentUserName, System.StringComparison.
                    OrdinalIgnoreCase))
                {
                    return lobbyMember.IsHost;
                }

                if (lobbyMember.UserId == currentUserId)
                {
                    return lobbyMember.IsHost;
                }
            }

            if (!string.IsNullOrWhiteSpace(hostUserName) &&
                string.Equals(hostUserName, currentUserName, System.StringComparison.
                OrdinalIgnoreCase))
            {
                return true;
            }

            return hostUserId == currentUserId;
        }

        public static string BuildStatusText(
            string codigoPartida,
            string hostUserName,
            int membersCount,
            byte maxPlayers,
            string lobbyStatus)
        {
            return $"Lobby {codigoPartida} — Host: {hostUserName} — {membersCount}/{maxPlayers}" +
                $" — {lobbyStatus}";
        }
    }
}
