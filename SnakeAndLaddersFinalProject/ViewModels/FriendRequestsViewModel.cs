using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class FriendRequestsViewModel
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FriendRequestsViewModel));

        public ObservableCollection<FriendRequestItemDto> IncomingRequests { get; } =
            new ObservableCollection<FriendRequestItemDto>();

        public ObservableCollection<FriendRequestItemDto> OutgoingRequests { get; } =
            new ObservableCollection<FriendRequestItemDto>();

        public void LoadData()
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    IncomingRequests.Clear();
                    foreach (FriendRequestItemDto request in friendsApi.GetIncoming())
                    {
                        IncomingRequests.Add(request);
                    }

                    OutgoingRequests.Clear();
                    foreach (FriendRequestItemDto request in friendsApi.GetOutgoing())
                    {
                        OutgoingRequests.Add(request);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading friend requests.", ex);
                MessageBox.Show(Lang.errorLoadingRequestsText, Lang.errorTitle);
            }
        }

        public void AcceptRequest(FriendRequestItemDto requestItem)
        {
            if (requestItem == null)
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

                IncomingRequests.Remove(requestItem);
                MessageBox.Show(Lang.friendAcceptedText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error accepting friend request.", ex);

                LoadData();

                bool stillExists = IncomingRequests.Any(r => r.FriendLinkId == friendLinkId);

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

        public void RejectRequest(FriendRequestItemDto requestItem)
        {
            if (requestItem == null)
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

                IncomingRequests.Remove(requestItem);
                MessageBox.Show(Lang.friendRejectedText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error rejecting friend request.", ex);

                LoadData();

                bool stillExists = IncomingRequests.Any(r => r.FriendLinkId == friendLinkId);

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

        public void CancelRequest(FriendRequestItemDto requestItem)
        {
            if (requestItem == null)
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

                OutgoingRequests.Remove(requestItem);
                MessageBox.Show(Lang.friendRequestCanceledText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error canceling friend request.", ex);

                LoadData();

                bool stillExists = OutgoingRequests.Any(r => r.FriendLinkId == friendLinkId);

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
    }
}
