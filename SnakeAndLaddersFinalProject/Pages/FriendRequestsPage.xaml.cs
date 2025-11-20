using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using log4net;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class FriendRequestsPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FriendRequestsPage));

        private readonly ObservableCollection<FriendRequestItemDto> _incomingRequests =
            new ObservableCollection<FriendRequestItemDto>();

        private readonly ObservableCollection<FriendRequestItemDto> _outgoingRequests =
            new ObservableCollection<FriendRequestItemDto>();

        public FriendRequestsPage()
        {
            InitializeComponent();

            tvIncoming.ItemsSource = _incomingRequests;
            tvOutgoing.ItemsSource = _outgoingRequests;

            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            LoadData();
        }

        private void LoadData()
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    _incomingRequests.Clear();
                    foreach (FriendRequestItemDto request in friendsApi.GetIncoming())
                    {
                        _incomingRequests.Add(request);
                    }

                    _outgoingRequests.Clear();
                    foreach (FriendRequestItemDto request in friendsApi.GetOutgoing())
                    {
                        _outgoingRequests.Add(request);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading requests.", ex);
                MessageBox.Show(Lang.errorLoadingRequestsText, Lang.errorTitle);
            }
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (!(((FrameworkElement)sender).DataContext is FriendRequestItemDto requestItem))
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    friendsApi.Accept(requestItem.FriendLinkId);
                }

                _incomingRequests.Remove(requestItem);
                MessageBox.Show(Lang.friendAcceptedText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error accepting request.", ex);
                MessageBox.Show(Lang.errorAcceptingRequestText, Lang.errorTitle);
            }
        }

        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (!(((FrameworkElement)sender).DataContext is FriendRequestItemDto requestItem))
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    friendsApi.Reject(requestItem.FriendLinkId);
                }

                _incomingRequests.Remove(requestItem);
                MessageBox.Show(Lang.friendRejectedText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error rejecting request.", ex);
                MessageBox.Show(Lang.errorRejectingRequestText, Lang.errorTitle);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (!(((FrameworkElement)sender).DataContext is FriendRequestItemDto requestItem))
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    friendsApi.Cancel(requestItem.FriendLinkId);
                }

                _outgoingRequests.Remove(requestItem);
                MessageBox.Show(Lang.friendRequestCanceledText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error canceling request.", ex);
                MessageBox.Show(Lang.errorCancelingRequestText, Lang.errorTitle);
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
