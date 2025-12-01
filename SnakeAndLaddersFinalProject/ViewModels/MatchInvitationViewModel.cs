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

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchInvitationViewModel));

        private readonly MatchInvitationServiceClient invitationClient;
        private readonly FriendsListViewModel friendsViewModel;

        private bool isBusy;
        private string statusText;
        private string gameCode;
        private int lobbyId;
        private FriendListItemDto selectedFriend;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action RequestClose;
        public event Action<string, string, MessageBoxImage> ShowMessageRequested;

        public MatchInvitationViewModel(int lobbyId, string gameCode)
        {
            this.lobbyId = lobbyId;
            this.gameCode = (gameCode ?? string.Empty).Trim();

            invitationClient = new MatchInvitationServiceClient(MATCH_INVITATION_BASIC_ENDPOINT);
            friendsViewModel = new FriendsListViewModel();

            Friends = friendsViewModel.Friends;

            SendInvitationCommand = new AsyncCommand(SendInvitationAsync, () => CanSendInvitation);

            StatusText = string.Empty;

            if (SessionGuard.HasValidSession())
            {
                try
                {
                    friendsViewModel.LoadFriends();

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
                        Logger);

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
            get { return selectedFriend; }
            set
            {
                if (selectedFriend == value)
                {
                    return;
                }

                selectedFriend = value;
                OnPropertyChanged();
                RaiseCanExecutes();
            }
        }

        public string GameCode
        {
            get { return gameCode; }
            private set
            {
                if (gameCode == value)
                {
                    return;
                }

                gameCode = value;
                OnPropertyChanged();
                RaiseCanExecutes();
            }
        }

        public int LobbyId
        {
            get { return lobbyId; }
            private set
            {
                if (lobbyId == value)
                {
                    return;
                }

                lobbyId = value;
                OnPropertyChanged();
                RaiseCanExecutes();
            }
        }

        public string StatusText
        {
            get { return statusText; }
            private set
            {
                if (statusText == value)
                {
                    return;
                }

                statusText = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            private set
            {
                if (isBusy == value)
                {
                    return;
                }

                isBusy = value;
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

                Logger.InfoFormat(
                    "Sending game invitation. FriendUserId={0}, GameCode={1}",
                    request.FriendUserId,
                    request.GameCode);

                OperationResult result = await invitationClient.InviteFriendToGameAsync(request);

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
                    Logger);

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
