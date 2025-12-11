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
        private const int USERNAME_MIN_LENGTH = 3;
        private const int USERNAME_MAX_LENGTH = 50;

        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(FriendRequestsViewModel));

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
                using (FriendsApi friendsApi = new FriendsApi())
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
                string genericMessage = ExceptionHandler.Handle(
                    ex,
                    nameof(LoadData),
                    _logger);

                string finalMessage = string.Format(
                    "{0} {1}",
                    genericMessage,
                    Lang.errorLoadingRequestsText);

                MessageBox.Show(
                    finalMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                if (ConnectionLostHandlerException.IsConnectionException(ex))
                {
                    ConnectionLostHandlerException.HandleConnectionLost();
                }
            }
        }

        public static bool ValidateSearchUsername(string username, out string normalizedUsername)
        {
            normalizedUsername = InputValidator.Normalize(username);

            bool isValid = InputValidator.IsIdentifierText(
                normalizedUsername,
                USERNAME_MIN_LENGTH,
                USERNAME_MAX_LENGTH);

            return isValid;
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
                using (FriendsApi friendsApi = new FriendsApi())
                {
                    friendsApi.Accept(friendLinkId);
                }

                IncomingRequests.Remove(requestItem);

                MessageBox.Show(
                    Lang.friendAcceptedText,
                    Lang.infoTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string genericMessage = ExceptionHandler.Handle(
                    ex,
                    nameof(AcceptRequest),
                    _logger);

                LoadData();

                bool stillExists = IncomingRequests.Any(r => r.FriendLinkId == friendLinkId);

                if (!stillExists)
                {
                    MessageBox.Show(
                        Lang.friendRequestNoLongerExistsText,
                        Lang.infoTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    string finalMessage = string.Format(
                        "{0} {1}",
                        genericMessage,
                        Lang.errorAcceptingRequestText);

                    MessageBox.Show(
                        finalMessage,
                        Lang.errorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                if (ConnectionLostHandlerException.IsConnectionException(ex))
                {
                    ConnectionLostHandlerException.HandleConnectionLost();
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
                using (FriendsApi friendsApi = new FriendsApi())
                {
                    friendsApi.Reject(friendLinkId);
                }

                IncomingRequests.Remove(requestItem);

                MessageBox.Show(
                    Lang.friendRejectedText,
                    Lang.infoTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string genericMessage = ExceptionHandler.Handle(
                    ex,
                    nameof(RejectRequest),
                    _logger);

                LoadData();

                bool stillExists = IncomingRequests.Any(r => r.FriendLinkId == friendLinkId);

                if (!stillExists)
                {
                    MessageBox.Show(
                        Lang.friendRequestNoLongerExistsText,
                        Lang.infoTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    string finalMessage = string.Format(
                        "{0} {1}",
                        genericMessage,
                        Lang.errorRejectingRequestText);

                    MessageBox.Show(
                        finalMessage,
                        Lang.errorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                if (ConnectionLostHandlerException.IsConnectionException(ex))
                {
                    ConnectionLostHandlerException.HandleConnectionLost();
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
                using (FriendsApi friendsApi = new FriendsApi())
                {
                    friendsApi.Cancel(friendLinkId);
                }

                OutgoingRequests.Remove(requestItem);

                MessageBox.Show(
                    Lang.friendRequestCanceledText,
                    Lang.infoTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string genericMessage = ExceptionHandler.Handle(
                    ex,
                    nameof(CancelRequest),
                    _logger);

                LoadData();

                bool stillExists = OutgoingRequests.Any(r => r.FriendLinkId == friendLinkId);

                if (!stillExists)
                {
                    MessageBox.Show(
                        Lang.friendRequestNoLongerExistsText,
                        Lang.infoTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    string finalMessage = string.Format(
                        "{0} {1}",
                        genericMessage,
                        Lang.errorCancelingRequestText);

                    MessageBox.Show(
                        finalMessage,
                        Lang.errorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                if (ConnectionLostHandlerException.IsConnectionException(ex))
                {
                    ConnectionLostHandlerException.HandleConnectionLost();
                }
            }
        }
    }
}
