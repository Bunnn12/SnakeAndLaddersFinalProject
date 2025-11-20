using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using log4net;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class FriendsListPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FriendsListPage));

        private const string CONFIRM_UNFRIEND_TITLE = "Confirm action";
        private const string CONFIRM_UNFRIEND_MESSAGE = "Remove this user from your friends list?";
        private const string GENERIC_ERROR_TITLE = "Error";

        private readonly ObservableCollection<FriendListItemDto> _friends =
            new ObservableCollection<FriendListItemDto>();

        public FriendsListPage()
        {
            InitializeComponent();

            tvFriends.ItemsSource = _friends;

            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            LoadFriends();
        }

        private void tvFriends_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            DependencyObject element = Mouse.DirectlyOver as DependencyObject;

            while (element != null && !(element is DataGridRow))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            if (element is DataGridRow row)
            {
                row.IsSelected = true;
            }
        }

        private void TvFriends_RowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (tvFriends.SelectedItem is FriendListItemDto friendItem)
            {
                TryUnfriend(friendItem);
            }
        }

        private void TvFriends_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (tvFriends.SelectedItem is FriendListItemDto friendItem)
            {
                if (e.Key == Key.Enter || e.Key == Key.Delete)
                {
                    e.Handled = true;
                    TryUnfriend(friendItem);
                }
            }
        }

        private void TryUnfriend(FriendListItemDto friendItem)
        {
            if (friendItem == null)
            {
                return;
            }

            string title = TryGetLangOr(CONFIRM_UNFRIEND_TITLE, language: Lang.btnUnfriendText);
            string message = TryGetLangOr(CONFIRM_UNFRIEND_MESSAGE, "language: Lang.confirmUnfriendText");

            MessageBoxResult result = MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    friendsApi.Remove(friendItem.FriendLinkId);
                }

                _friends.Remove(friendItem);
                MessageBox.Show(Lang.friendRemovedOkText, Lang.infoTitle);
            }
            catch (FaultException ex)
            {
                Logger.WarnFormat("Unfriend fault: {0} - {1}", ex.Code, ex.Message);
                MessageBox.Show(ex.Message, Lang.errorTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error removing friend.", ex);
                MessageBox.Show(Lang.errorRemovingFriendText ?? GENERIC_ERROR_TITLE, Lang.errorTitle);
            }
        }

        private void LoadFriends()
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    _friends.Clear();

                    foreach (FriendListItemDto friendItem in friendsApi.GetFriends())
                    {
                        _friends.Add(friendItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed loading friends.", ex);
                MessageBox.Show(Lang.errorLoadingFriendsListText, Lang.errorTitle);
            }
        }

        private void BtnUnfriend_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (!(((FrameworkElement)sender).DataContext is FriendListItemDto friendItem))
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    friendsApi.Remove(friendItem.FriendLinkId);
                }

                _friends.Remove(friendItem);
                MessageBox.Show(Lang.friendRemovedOkText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error removing friend.", ex);
                MessageBox.Show(Lang.errorRemovingFriendText, Lang.errorTitle);
            }
        }

        private void ContextUnfriend_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (tvFriends.SelectedItem is FriendListItemDto friendItem)
            {
                TryUnfriend(friendItem);
            }
        }

        private void BtnAddFriends_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AddFriendsPage());
        }

        private void BtnFriendRequests_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new FriendRequestsPage());
        }

        private static string TryGetLangOr(string fallback, string language) =>
            string.IsNullOrWhiteSpace(language) ? fallback : language;

        private void Back(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}
