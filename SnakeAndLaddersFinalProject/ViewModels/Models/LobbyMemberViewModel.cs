namespace SnakeAndLaddersFinalProject.ViewModels.Models
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public sealed class LobbyMemberViewModel : INotifyPropertyChanged
    {
        public int UserId { get; }
        public string UserName { get; }
        public DateTime JoinedAt { get; }

        private bool _isHost;
        public bool IsHost
        {
            get => _isHost;
            set { _isHost = value; OnPropertyChanged(); }
        }

        public LobbyMemberViewModel(int userId, string userName, bool isHost, DateTime joinedAt)
        {
            UserId = userId;
            UserName = userName;
            IsHost = isHost;
            JoinedAt = joinedAt;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
