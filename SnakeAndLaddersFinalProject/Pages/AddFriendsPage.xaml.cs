using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class AddFriendsPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddFriendsPage));

        private const int DEBOUNCE_MS = 250;
        private const int SEARCH_MAX_RESULTS = 20;
        private const int MIN_SEARCH_TERM_LENGTH = 2;

        private const string AUTO_ACCEPTED_FRIEND_MESSAGE =
            "This user had already sended a friend request to you, both now are friends";

        private readonly ObservableCollection<UserBriefDto> _searchResults =
            new ObservableCollection<UserBriefDto>();

        private readonly DispatcherTimer _searchDebounceTimer;

        public AddFriendsPage()
        {
            InitializeComponent();

            tvSearchResults.ItemsSource = _searchResults;

            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(DEBOUNCE_MS)
            };
            _searchDebounceTimer.Tick += (_, __) =>
            {
                _searchDebounceTimer.Stop();
                RunSearch(txtFindFriend.Text);
            };
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            txtFindFriend.Focus();
            Keyboard.Focus(txtFindFriend);
            txtFindFriend.CaretIndex = txtFindFriend.Text?.Length ?? 0;
        }

        private void FindFriendTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void RunSearch(string term)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            term = (term ?? string.Empty).Trim();
            if (term.Length < MIN_SEARCH_TERM_LENGTH)
            {
                _searchResults.Clear();
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    _searchResults.Clear();

                    foreach (UserBriefDto user in friendsApi.SearchUsers(term, SEARCH_MAX_RESULTS))
                    {
                        _searchResults.Add(user);
                    }
                }
            }
            
            catch (Exception ex)
            {
                
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(AddFriendsPage)}.{nameof(RunSearch)}",
                    Logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddFriend(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (!(((FrameworkElement)sender).DataContext is UserBriefDto user))
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    FriendLinkDto link = friendsApi.SendFriendRequest(user.UserId);
                    _searchResults.Remove(user);

                    if (link != null && link.Status == FriendRequestStatus.Accepted)
                    {
                        MessageBox.Show(
                            AUTO_ACCEPTED_FRIEND_MESSAGE,
                            Lang.infoTitle);
                    }
                    else
                    {
                        MessageBox.Show(
                            Lang.friendRequestSentText,
                            Lang.infoTitle);
                    }
                }
            }

            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(AddFriendsPage)}.{nameof(AddFriend)}",
                    Logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Back(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}
