using SnakeAndLaddersFinalProject.Game;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SnakeAndLaddersFinalProject.Managers
{
    public sealed class LobbyPlayersManager
    {
        private readonly int localUserId;
        private readonly CornerPlayersViewModel cornerPlayers;
        private readonly PlayerTokenManager tokenManager;
        private readonly int startCellIndex;

        private readonly Dictionary<int, string> userNamesById =
            new Dictionary<int, string>();

        private readonly List<LobbyMemberViewModel> lobbyMembers =
            new List<LobbyMemberViewModel>();

        public LobbyPlayersManager(
            int localUserId,
            CornerPlayersViewModel cornerPlayers,
            PlayerTokenManager tokenManager,
            int startCellIndex)
        {
            if (cornerPlayers == null)
            {
                throw new ArgumentNullException(nameof(cornerPlayers));
            }

            if (tokenManager == null)
            {
                throw new ArgumentNullException(nameof(tokenManager));
            }

            if (startCellIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startCellIndex));
            }

            this.localUserId = localUserId;
            this.cornerPlayers = cornerPlayers;
            this.tokenManager = tokenManager;
            this.startCellIndex = startCellIndex;
        }

        public IReadOnlyList<LobbyMemberViewModel> LobbyMembers
        {
            get { return lobbyMembers.AsReadOnly(); }
        }

        public void InitializeCornerPlayers(IList<LobbyMemberViewModel> members)
        {
            if (members == null)
            {
                return;
            }

            lobbyMembers.Clear();
            userNamesById.Clear();

            foreach (LobbyMemberViewModel member in members)
            {
                member.IsLocalPlayer = member.UserId == localUserId;

                lobbyMembers.Add(member);
                userNamesById[member.UserId] = member.UserName ?? string.Empty;
            }

            cornerPlayers.InitializeFromLobbyMembers(members);
        }

        public string ResolveUserDisplayName(int userId)
        {
            if (userNamesById.TryGetValue(userId, out string name) &&
                !string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            return string.Format("Jugador {0}", userId);
        }

        public ReadOnlyCollection<PodiumPlayerViewModel> BuildPodiumPlayers(int winnerUserId)
        {
            List<PodiumPlayerViewModel> result = new List<PodiumPlayerViewModel>();

            if (lobbyMembers.Count == 0)
            {
                return new ReadOnlyCollection<PodiumPlayerViewModel>(result);
            }

            LobbyMemberViewModel winner =
                lobbyMembers.FirstOrDefault(m => m.UserId == winnerUserId);

            if (winner != null)
            {
                result.Add(
                    new PodiumPlayerViewModel(
                        winner.UserId,
                        winner.UserName,
                        1,
                        0));
            }

            foreach (LobbyMemberViewModel member in lobbyMembers)
            {
                if (member.UserId == winnerUserId)
                {
                    continue;
                }

                if (result.Count >= 3)
                {
                    break;
                }

                int position = result.Count + 1;

                result.Add(
                    new PodiumPlayerViewModel(
                        member.UserId,
                        member.UserName,
                        position,
                        0));
            }

            return new ReadOnlyCollection<PodiumPlayerViewModel>(result);
        }

        public void InitializeTokensFromLobbyMembers(IList<LobbyMemberViewModel> members)
        {
            tokenManager.PlayerTokens.Clear();

            if (members == null || members.Count == 0)
            {
                return;
            }

            foreach (LobbyMemberViewModel lobbyMember in members)
            {
                tokenManager.CreateFromLobbyMember(
                    lobbyMember,
                    startCellIndex);
            }

            tokenManager.ResetAllTokensToCell(startCellIndex);
        }
    }
}
