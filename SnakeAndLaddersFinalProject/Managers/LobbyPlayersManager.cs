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
        private const int MIN_START_CELL_INDEX = 1;
        private const int MAX_PODIUM_PLAYERS = 3;
        private const int FIRST_PODIUM_POSITION = 1;

        private readonly int _localUserId;
        private readonly CornerPlayersViewModel _cornerPlayers;
        private readonly PlayerTokenManager _tokenManager;
        private readonly int _startCellIndex;

        private readonly Dictionary<int, string> _userNamesById = new Dictionary<int, string>();
        private readonly List<LobbyMemberViewModel> _lobbyMembers =
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

            if (startCellIndex < MIN_START_CELL_INDEX)
            {
                throw new ArgumentOutOfRangeException(nameof(startCellIndex));
            }

            _localUserId = localUserId;
            _cornerPlayers = cornerPlayers;
            _tokenManager = tokenManager;
            _startCellIndex = startCellIndex;
        }

        public IReadOnlyList<LobbyMemberViewModel> LobbyMembers
        {
            get { return _lobbyMembers.AsReadOnly(); }
        }

        public void InitializeCornerPlayers(IList<LobbyMemberViewModel> members)
        {
            if (members == null)
            {
                return;
            }

            _lobbyMembers.Clear();
            _userNamesById.Clear();

            foreach (var member in members)
            {
                member.IsLocalPlayer = member.UserId == _localUserId;
                _lobbyMembers.Add(member);

                string userName = member.UserName ?? string.Empty;
                _userNamesById[member.UserId] = userName;
            }

            _cornerPlayers.InitializeFromLobbyMembers(members);
        }

        public string ResolveUserDisplayName(int userId)
        {
            if (_userNamesById.TryGetValue(userId, out string name) && !string.
                IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            return string.Format("Jugador {0}", userId);
        }

        public ReadOnlyCollection<PodiumPlayerViewModel> BuildPodiumPlayers(int winnerUserId)
        {
            var result = new List<PodiumPlayerViewModel>();

            if (_lobbyMembers.Count == 0)
            {
                return new ReadOnlyCollection<PodiumPlayerViewModel>(result);
            }

            var winner = _lobbyMembers.FirstOrDefault(m => m.UserId == winnerUserId);
            if (winner != null)
            {
                result.Add(new PodiumPlayerViewModel(
                    winner.UserId,
                    winner.UserName,
                    FIRST_PODIUM_POSITION,
                    0));
            }

            foreach (var member in _lobbyMembers)
            {
                if (member.UserId == winnerUserId)
                {
                    continue;
                }

                if (result.Count >= MAX_PODIUM_PLAYERS)
                {
                    break;
                }

                int position = result.Count + 1;
                result.Add(new PodiumPlayerViewModel(
                    member.UserId,
                    member.UserName,
                    position,
                    0));
            }

            return new ReadOnlyCollection<PodiumPlayerViewModel>(result);
        }

        public void InitializeTokensFromLobbyMembers(IList<LobbyMemberViewModel> members)
        {
            _tokenManager.PlayerTokens.Clear();

            if (members == null || members.Count == 0)
            {
                return;
            }

            foreach (var lobbyMember in members)
            {
                _tokenManager.CreateFromLobbyMember(lobbyMember, _startCellIndex);
            }

            _tokenManager.ResetAllTokensToCell(_startCellIndex);
        }
    }
}
