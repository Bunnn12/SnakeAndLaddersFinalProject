using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class CornerPlayersViewModel : INotifyPropertyChanged
    {
        private const int MAX_CORNER_PLAYERS = 4;

        private LobbyMemberViewModel topLeftPlayer;
        private LobbyMemberViewModel topRightPlayer;
        private LobbyMemberViewModel bottomLeftPlayer;
        private LobbyMemberViewModel bottomRightPlayer;

        public LobbyMemberViewModel TopLeftPlayer
        {
            get { return topLeftPlayer; }
            private set
            {
                if (topLeftPlayer == value)
                {
                    return;
                }

                topLeftPlayer = value;
                OnPropertyChanged();
            }
        }

        public LobbyMemberViewModel TopRightPlayer
        {
            get { return topRightPlayer; }
            private set
            {
                if (topRightPlayer == value)
                {
                    return;
                }

                topRightPlayer = value;
                OnPropertyChanged();
            }
        }

        public LobbyMemberViewModel BottomLeftPlayer
        {
            get { return bottomLeftPlayer; }
            private set
            {
                if (bottomLeftPlayer == value)
                {
                    return;
                }

                bottomLeftPlayer = value;
                OnPropertyChanged();
            }
        }

        public LobbyMemberViewModel BottomRightPlayer
        {
            get { return bottomRightPlayer; }
            private set
            {
                if (bottomRightPlayer == value)
                {
                    return;
                }

                bottomRightPlayer = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void InitializeFromLobbyMembers(IList<LobbyMemberViewModel> members)
        {
            if (members == null || members.Count == 0)
            {
                ClearCornerPlayers();
                return;
            }

            int total = members.Count > MAX_CORNER_PLAYERS
                ? MAX_CORNER_PLAYERS
                : members.Count;

            LobbyMemberViewModel member0 = total > 0 ? members[0] : null;
            LobbyMemberViewModel member1 = total > 1 ? members[1] : null;
            LobbyMemberViewModel member2 = total > 2 ? members[2] : null;
            LobbyMemberViewModel member3 = total > 3 ? members[3] : null;

            TopLeftPlayer = member0;
            TopRightPlayer = member1;
            BottomLeftPlayer = member2;
            BottomRightPlayer = member3;
        }

        private void ClearCornerPlayers()
        {
            TopLeftPlayer = null;
            TopRightPlayer = null;
            BottomLeftPlayer = null;
            BottomRightPlayer = null;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateCurrentTurn(int currentTurnUserId)
        {
            SetCurrentTurnForPlayer(TopLeftPlayer, currentTurnUserId);
            SetCurrentTurnForPlayer(TopRightPlayer, currentTurnUserId);
            SetCurrentTurnForPlayer(BottomLeftPlayer, currentTurnUserId);
            SetCurrentTurnForPlayer(BottomRightPlayer, currentTurnUserId);
        }

        private static void SetCurrentTurnForPlayer(
            LobbyMemberViewModel player,
            int currentTurnUserId)
        {
            if (player == null)
            {
                return;
            }

            player.IsCurrentTurn = player.UserId == currentTurnUserId;
        }
    }
}
