using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        private readonly ObservableCollection<FriendRequestItemDto> incomingRequests =
            new ObservableCollection<FriendRequestItemDto>();

        private readonly ObservableCollection<FriendRequestItemDto> outgoingRequests =
            new ObservableCollection<FriendRequestItemDto>();

        public FriendRequestsPage()
        {
            InitializeComponent();

            tvIncoming.ItemsSource = incomingRequests;
            tvOutgoing.ItemsSource = outgoingRequests;

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
                    incomingRequests.Clear();
                    foreach (FriendRequestItemDto request in friendsApi.GetIncoming())
                    {
                        incomingRequests.Add(request);
                    }

                    outgoingRequests.Clear();
                    foreach (FriendRequestItemDto request in friendsApi.GetOutgoing())
                    {
                        outgoingRequests.Add(request);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading friend requests.", ex);
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

            int friendLinkId = requestItem.FriendLinkId;

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    friendsApi.Accept(friendLinkId);
                }

                incomingRequests.Remove(requestItem);
                MessageBox.Show(Lang.friendAcceptedText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error accepting friend request.", ex);

                LoadData();

                bool stillExists = incomingRequests.Any(r => r.FriendLinkId == friendLinkId);

                if (!stillExists)
                {
                    MessageBox.Show(
                        "Lang.friendRequestNoLongerExistsText",
                        Lang.infoTitle);
                }
                else
                {
                    MessageBox.Show(
                        Lang.errorAcceptingRequestText,
                        Lang.errorTitle);
                }
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

            int friendLinkId = requestItem.FriendLinkId;

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    friendsApi.Reject(friendLinkId);
                }

                incomingRequests.Remove(requestItem);
                MessageBox.Show(Lang.friendRejectedText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error rejecting friend request.", ex);

                LoadData();

                bool stillExists = incomingRequests.Any(r => r.FriendLinkId == friendLinkId);

                if (!stillExists)
                {
                    MessageBox.Show(
                        "Lang.friendRequestNoLongerExistsText",
                        Lang.infoTitle);
                }
                else
                {
                    MessageBox.Show(
                        Lang.errorRejectingRequestText,
                        Lang.errorTitle);
                }
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

            int friendLinkId = requestItem.FriendLinkId;

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    friendsApi.Cancel(friendLinkId);
                }

                outgoingRequests.Remove(requestItem);
                MessageBox.Show(Lang.friendRequestCanceledText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error canceling friend request.", ex);

                LoadData();

                bool stillExists = outgoingRequests.Any(r => r.FriendLinkId == friendLinkId);

                if (!stillExists)
                {
                    MessageBox.Show(
                        "Lang.friendRequestNoLongerExistsText",
                        Lang.infoTitle);
                }
                else
                {
                    MessageBox.Show(
                        Lang.errorCancelingRequestText,
                        Lang.errorTitle);
                }
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
