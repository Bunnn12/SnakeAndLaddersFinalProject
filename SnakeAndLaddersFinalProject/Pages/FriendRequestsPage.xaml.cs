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

        private readonly ObservableCollection<FriendRequestItemDto> incoming = new ObservableCollection<FriendRequestItemDto>();
        private readonly ObservableCollection<FriendRequestItemDto> outgoing = new ObservableCollection<FriendRequestItemDto>();

        public FriendRequestsPage()
        {
            InitializeComponent();
            tvIncoming.ItemsSource = incoming;
            tvOutgoing.ItemsSource = outgoing;

            if (!SessionGuard.HasValidSession()) return;

            LoadData();
        }

        private void LoadData()
        {
            if (!SessionGuard.HasValidSession()) return;

            try
            {
                using (var api = new FriendsApi())
                {
                    incoming.Clear();
                    foreach (var r in api.GetIncoming()) incoming.Add(r);

                    outgoing.Clear();
                    foreach (var r in api.GetOutgoing()) outgoing.Add(r);
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
            if (!SessionGuard.HasValidSession()) return;

            if (((FrameworkElement)sender).DataContext is FriendRequestItemDto item)
            {
                try
                {
                    using (var api = new FriendsApi())
                    {
                        api.Accept(item.FriendLinkId);
                    }
                    incoming.Remove(item);
                    MessageBox.Show(Lang.friendAcceptedText, Lang.infoTitle);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error accepting request.", ex);
                    MessageBox.Show(Lang.errorAcceptingRequestText, Lang.errorTitle);
                }
            }
        }

        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession()) return;

            if (((FrameworkElement)sender).DataContext is FriendRequestItemDto item)
            {
                try
                {
                    using (var api = new FriendsApi())
                    {
                        api.Reject(item.FriendLinkId);
                    }
                    incoming.Remove(item);
                    MessageBox.Show(Lang.friendRejectedText, Lang.infoTitle);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error rejecting request.", ex);
                    MessageBox.Show(Lang.errorRejectingRequestText, Lang.errorTitle);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession()) return;

            if (((FrameworkElement)sender).DataContext is FriendRequestItemDto item)
            {
                try
                {
                    using (var api = new FriendsApi())
                    {
                        api.Cancel(item.FriendLinkId);
                    }
                    outgoing.Remove(item);
                    MessageBox.Show(Lang.friendRequestCanceledText, Lang.infoTitle);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error canceling request.", ex);
                    MessageBox.Show(Lang.errorCancelingRequestText, Lang.errorTitle);
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
