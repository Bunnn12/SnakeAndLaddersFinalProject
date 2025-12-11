using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class FriendRequestsPage : Page
    {
        private FriendRequestsViewModel _viewModel
        {
            get { return DataContext as FriendRequestsViewModel; }
        }

        public FriendRequestsPage()
        {
            InitializeComponent();

            DataContext = new FriendRequestsViewModel();

            if (_viewModel != null)
            {
                tvIncoming.ItemsSource = _viewModel.IncomingRequests;
                tvOutgoing.ItemsSource = _viewModel.OutgoingRequests;
            }

            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            _viewModel?.LoadData();
        }

        private void AcceptRequest(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (!(((FrameworkElement)sender).DataContext is FriendRequestItemDto requestItem))
            {
                return;
            }

            _viewModel?.AcceptRequest(requestItem);
        }

        private void RejectRequest(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (!(((FrameworkElement)sender).DataContext is FriendRequestItemDto requestItem))
            {
                return;
            }

            _viewModel?.RejectRequest(requestItem);
        }

        private void CancelRequest(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (!(((FrameworkElement)sender).DataContext is FriendRequestItemDto requestItem))
            {
                return;
            }

            _viewModel?.CancelRequest(requestItem);
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
