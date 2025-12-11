using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Pages;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class FriendsListPage : Page
    {
        private const int MIN_VALID_USER_ID = 1;

        private FriendsListViewModel ViewModel
        {
            get { return DataContext as FriendsListViewModel; }
        }

        public FriendsListPage()
        {
            InitializeComponent();

            DataContext = new FriendsListViewModel();

            if (ViewModel != null)
            {
                tvFriends.ItemsSource = ViewModel.Friends;
            }

            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            ViewModel?.LoadFriends();
        }

        private void TvFriendsRowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (tvFriends.SelectedItem is FriendListItemDto friendItem)
            {
                ViewModel?.UnfriendWithConfirmation(friendItem);
            }
        }

        private void TvFriendsPreviewKeyDown(object sender, KeyEventArgs e)
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
                    ViewModel?.UnfriendWithConfirmation(friendItem);
                }
            }
        }

        private void FriendMenu(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (!(sender is FrameworkElement element))
            {
                return;
            }

            if (element.DataContext is FriendListItemDto friendItem)
            {
                tvFriends.SelectedItem = friendItem;
            }

            ContextMenu contextMenu = element.ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.PlacementTarget = element;
                contextMenu.IsOpen = true;
            }
        }

        private void Unfriend(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (tvFriends.SelectedItem is FriendListItemDto friendItem)
            {
                ViewModel?.UnfriendWithConfirmation(friendItem);
            }
        }

        private void ViewStats(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            FriendListItemDto friendItem = null;

            if (sender is FrameworkElement element &&
                element.DataContext is FriendListItemDto ctxItem)
            {
                friendItem = ctxItem;
            }
            else if (tvFriends.SelectedItem is FriendListItemDto selectedItem)
            {
                friendItem = selectedItem;
            }

            if (friendItem == null || friendItem.FriendUserId < MIN_VALID_USER_ID)
            {
                return;
            }

            var statsPage = new ProfileStatsPage(
                friendItem.FriendUserId,
                friendItem.FriendUserName,
                friendItem.ProfilePhotoId);

            NavigationService?.Navigate(statsPage);
        }

        private void AddFriends(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AddFriendsPage());
        }

        private void FriendRequests(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new FriendRequestsPage());
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
