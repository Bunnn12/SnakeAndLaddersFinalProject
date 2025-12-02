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
            IEnumerable<LobbyMemberViewModel> members)
        {
            foreach (var member in members)
            {
                if (member == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(member.UserName) &&
                    string.Equals(member.UserName, currentUserName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return member.IsHost;
                }

                if (member.UserId == currentUserId)
                {
                    return member.IsHost;
                }
            }

            if (!string.IsNullOrWhiteSpace(hostUserName) &&
                string.Equals(hostUserName, currentUserName, System.StringComparison.OrdinalIgnoreCase))
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
            return $"Lobby {codigoPartida} — Host: {hostUserName} — " +
                   $"{membersCount}/{maxPlayers} — {lobbyStatus}";
        }
    }
}
