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

        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(MatchInvitationViewModel));

        private readonly MatchInvitationServiceClient _invitationClient;
        private readonly FriendsListViewModel _friendsViewModel;

        private bool _isBusy;
        private string _statusText;
        private string _gameCode;
        private int _lobbyId;
        private FriendListItemDto _selectedFriend;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<string, string, MessageBoxImage> ShowMessageRequested;
        public event Action RequestClose;

        public MatchInvitationViewModel(int lobbyId, string gameCode)
        {
            _lobbyId = lobbyId;
            _gameCode = (gameCode ?? string.Empty).Trim();

            _invitationClient = new MatchInvitationServiceClient(MATCH_INVITATION_BASIC_ENDPOINT);
            _friendsViewModel = new FriendsListViewModel();

            Friends = _friendsViewModel.Friends;

            SendInvitationCommand = new AsyncCommand(
                SendInvitationAsync,
                () => CanSendInvitation);

            StatusText = string.Empty;

            InitializeFriends();
        }

        private void InitializeFriends()
        {
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
                        "MatchInvitationViewModel.FriendsLoad",
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

        public bool CanSendInvitation
        {
            get { return !IsBusy && SessionGuard.HasValidSession(); }
        }

        public ICommand SendInvitationCommand { get; }

        private async Task SendInvitationAsync()
        {
            if (!CanSendInvitation)
            {
                return;
            }

            IsBusy = true;
            StatusText = string.Empty;

            try
            {
                if (!TryValidateInvitation(out string token))
                {
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

                OperationResult result =
                    await _invitationClient.InviteFriendToGameAsync(request);

                HandleInvitationResult(result);
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    "MatchInvitationViewModel.SendInvitationAsync",
                    _logger);

                StatusText = userMessage;

                OnShowMessageRequested(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxImage.Error);

                SafeAbort(_invitationClient, "SendInvitationAsync");
                OnRequestClose();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool TryValidateInvitation(out string token)
        {
            token = string.Empty;

            SessionContext session = SessionContext.Current;
            string sessionToken = session?.AuthToken;

            if (session == null || string.IsNullOrWhiteSpace(sessionToken))
            {
                return FailInvitation(
                    Lang.UiInviteFriendInvalidSession,
                    Lang.errorTitle,
                    MessageBoxImage.Error,
                    out token);
            }

            if (sessionToken.StartsWith(GUEST_TOKEN_PREFIX, StringComparison.Ordinal))
            {
                return FailInvitation(
                    Lang.UiInviteFriendGuestsNotAllowed,
                    Lang.warningTitle,
                    MessageBoxImage.Warning,
                    out token);
            }

            if (string.IsNullOrWhiteSpace(GameCode))
            {
                return FailInvitation(
                    Lang.UiInviteFriendMissingGameCode,
                    Lang.warningTitle,
                    MessageBoxImage.Warning,
                    out token);
            }

            if (Friends == null || Friends.Count == 0)
            {
                return FailInvitation(
                    Lang.InviteNoFriendsInfo,
                    Lang.infoTitle,
                    MessageBoxImage.Information,
                    out token);
            }

            if (SelectedFriend == null)
            {
                return FailInvitation(
                    Lang.UiInviteFriendSelectFriendRequired,
                    Lang.warningTitle,
                    MessageBoxImage.Warning,
                    out token);
            }

            token = sessionToken;
            return true;
        }

        private bool FailInvitation(
            string message,
            string title,
            MessageBoxImage icon,
            out string token)
        {
            token = string.Empty;
            StatusText = message;

            OnShowMessageRequested(
                message,
                title,
                icon);

            return false;
        }

        private void HandleInvitationResult(OperationResult result)
        {
            if (result == null)
            {
                StatusText = Lang.UiInviteFriendNoResponse;

                OnShowMessageRequested(
                    StatusText,
                    Lang.errorTitle,
                    MessageBoxImage.Error);

                return;
            }

            string message;

            if (result.Success)
            {
                message = string.IsNullOrWhiteSpace(result.Message)
                    ? Lang.UiInviteFriendSuccessDefault
                    : result.Message;

                StatusText = message;

                OnShowMessageRequested(
                    message,
                    Lang.infoTitle,
                    MessageBoxImage.Information);
            }
            else
            {
                message = string.IsNullOrWhiteSpace(result.Message)
                    ? Lang.UiInviteFriendFailureDefault
                    : result.Message;

                StatusText = message;

                OnShowMessageRequested(
                    message,
                    Lang.errorTitle,
                    MessageBoxImage.Error);
            }
        }

        private void RaiseCanExecutes()
        {
            (SendInvitationCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }

        private void OnShowMessageRequested(
            string message,
            string title,
            MessageBoxImage icon)
        {
            ShowMessageRequested?.Invoke(message, title, icon);
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnRequestClose()
        {
            RequestClose?.Invoke();
        }

        private static void SafeAbort(MatchInvitationServiceClient client, string operationName)
        {
            if (client == null)
            {
                return;
            }

            try
            {
                client.Abort();
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(
                    ex,
                    $"MatchInvitationViewModel.{operationName}.Abort",
                    _logger);
            }
        }
    }
}
