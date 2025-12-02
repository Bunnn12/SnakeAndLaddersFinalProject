using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.MatchInvitationService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class MatchInvitationViewModel : INotifyPropertyChanged
    {
        private const string MATCH_INVITATION_BASIC_ENDPOINT = "BasicHttpBinding_IMatchInvitationService";
        private const string GUEST_TOKEN_PREFIX = "GUEST-";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(MatchInvitationViewModel));

        private readonly MatchInvitationServiceClient _invitationClient;
        private readonly FriendsListViewModel _friendsViewModel;

        private bool _isBusy;
        private string _statusText;
        private string _gameCode;
        private int _lobbyId;
        private FriendListItemDto _selectedFriend;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action RequestClose;
        public event Action<string, string, MessageBoxImage> ShowMessageRequested;

        public MatchInvitationViewModel(int lobbyId, string gameCode)
        {
            this._lobbyId = lobbyId;
            this._gameCode = (gameCode ?? string.Empty).Trim();

            _invitationClient = new MatchInvitationServiceClient(MATCH_INVITATION_BASIC_ENDPOINT);
            _friendsViewModel = new FriendsListViewModel();

            Friends = _friendsViewModel.Friends;

            SendInvitationCommand = new AsyncCommand(SendInvitationAsync, () => CanSendInvitation);

            StatusText = string.Empty;

            if (SessionGuard.HasValidSession())
            {
                try
                {
                    _friendsViewModel.LoadFriends();

                    if (Friends == null || Friends.Count == 0)
                    {
                        StatusText = Lang.InviteNoFriendsInfo;
                        OnShowMessageRequested(
                            StatusText,
                            Lang.infoTitle,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    string userMessage = ExceptionHandler.Handle(
                        ex,
                        $"{nameof(MatchInvitationViewModel)}.FriendsLoad",
                        _logger);

                    StatusText = userMessage;
                    OnShowMessageRequested(
                        userMessage,
                        Lang.errorTitle,
                        MessageBoxImage.Error);
                }
            }
            else
            {
                StatusText = Lang.UiInviteFriendInvalidSession;
                OnShowMessageRequested(
                    StatusText,
                    Lang.errorTitle,
                    MessageBoxImage.Error);
            }
        }

        public ObservableCollection<FriendListItemDto> Friends { get; }

        public FriendListItemDto SelectedFriend
        {
            get { return _selectedFriend; }
            set
            {
                if (_selectedFriend == value)
                {
                    return;
                }

                _selectedFriend = value;
                OnPropertyChanged();
                RaiseCanExecutes();
            }
        }

        public string GameCode
        {
            get { return _gameCode; }
            private set
            {
                if (_gameCode == value)
                {
                    return;
                }

                _gameCode = value;
                OnPropertyChanged();
                RaiseCanExecutes();
            }
        }

        public int LobbyId
        {
            get { return _lobbyId; }
            private set
            {
                if (_lobbyId == value)
                {
                    return;
                }

                _lobbyId = value;
                OnPropertyChanged();
                RaiseCanExecutes();
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            private set
            {
                if (_statusText == value)
                {
                    return;
                }

                _statusText = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                OnPropertyChanged();
                RaiseCanExecutes();
            }
        }

        public bool CanSendInvitation =>
            !IsBusy &&
            SessionGuard.HasValidSession();

        public ICommand SendInvitationCommand { get; }

        private async Task SendInvitationAsync()
        {
            if (!CanSendInvitation)
            {
                return;
            }

            try
            {
                IsBusy = true;
                StatusText = string.Empty;

                var session = SessionContext.Current;
                string token = session?.AuthToken;

                if (session == null || string.IsNullOrWhiteSpace(token))
                {
                    StatusText = Lang.UiInviteFriendInvalidSession;
                    OnShowMessageRequested(
                        StatusText,
                        Lang.errorTitle,
                        MessageBoxImage.Error);
                    return;
                }

                if (token.StartsWith(GUEST_TOKEN_PREFIX, StringComparison.Ordinal))
                {
                    StatusText = Lang.UiInviteFriendGuestsNotAllowed;
                    OnShowMessageRequested(
                        StatusText,
                        Lang.warningTitle,
                        MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(GameCode))
                {
                    StatusText = Lang.UiInviteFriendMissingGameCode;
                    OnShowMessageRequested(
                        StatusText,
                        Lang.warningTitle,
                        MessageBoxImage.Warning);
                    return;
                }

                if (Friends == null || Friends.Count == 0)
                {
                    StatusText = Lang.InviteNoFriendsInfo;
                    OnShowMessageRequested(
                        StatusText,
                        Lang.infoTitle,
                        MessageBoxImage.Information);
                    return;
                }

                if (SelectedFriend == null)
                {
                    StatusText = Lang.UiInviteFriendSelectFriendRequired;
                    OnShowMessageRequested(
                        StatusText,
                        Lang.warningTitle,
                        MessageBoxImage.Warning);
                    return;
                }

                var request = new InviteFriendToGameRequestDto
                {
                    SessionToken = token,
                    FriendUserId = SelectedFriend.FriendUserId,
                    GameCode = GameCode
                };

                _logger.InfoFormat(
                    "Sending game invitation. FriendUserId={0}, GameCode={1}",
                    request.FriendUserId,
                    request.GameCode);

                OperationResult result = await _invitationClient.InviteFriendToGameAsync(request);

                if (result == null)
                {
                    StatusText = Lang.UiInviteFriendNoResponse;
                    OnShowMessageRequested(
                        StatusText,
                        Lang.errorTitle,
                        MessageBoxImage.Error);
                    return;
                }

                if (result.Success)
                {
                    string successMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? Lang.UiInviteFriendSuccessDefault
                        : result.Message;

                    StatusText = successMessage;
                    OnShowMessageRequested(
                        successMessage,
                        Lang.infoTitle,
                        MessageBoxImage.Information);
                }
                else
                {
                    string failureMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? Lang.UiInviteFriendFailureDefault
                        : result.Message;

                    StatusText = failureMessage;
                    OnShowMessageRequested(
                        failureMessage,
                        Lang.errorTitle,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(MatchInvitationViewModel)}.{nameof(SendInvitationAsync)}",
                    _logger);

                StatusText = userMessage;
                OnShowMessageRequested(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RaiseCanExecutes()
        {
            (SendInvitationCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }

        private void OnShowMessageRequested(string message, string title, MessageBoxImage icon)
        {
            ShowMessageRequested?.Invoke(message, title, icon);
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
