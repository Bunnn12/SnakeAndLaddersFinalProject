using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class FriendsListViewModel
    {
        private const string CONFIRM_UNFRIEND_TITLE = "Confirm action";
        private const string CONFIRM_UNFRIEND_MESSAGE = "Remove this user from your friends list?";
        private const string GENERIC_ERROR_TITLE = "Error";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FriendsListViewModel));

        public ObservableCollection<FriendListItemDto> Friends { get; } =
            new ObservableCollection<FriendListItemDto>();

        public void LoadFriends()
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    Friends.Clear();

                    foreach (FriendListItemDto friendItem in friendsApi.GetFriends())
                    {
                        Friends.Add(friendItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed loading friends.", ex);
                MessageBox.Show(Lang.errorLoadingFriendsListText, Lang.errorTitle);
            }
        }

        public void UnfriendWithConfirmation(FriendListItemDto friendItem)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (friendItem == null)
            {
                return;
            }

            string title = TryGetLangOr(CONFIRM_UNFRIEND_TITLE, Lang.btnUnfriendText);
            string message = TryGetLangOr(CONFIRM_UNFRIEND_MESSAGE, "Lang.confirmUnfriendText");

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

                Friends.Remove(friendItem);
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
                MessageBox.Show(
                    Lang.errorRemovingFriendText ?? GENERIC_ERROR_TITLE,
                    Lang.errorTitle);
            }
        }

        public void UnfriendDirect(FriendListItemDto friendItem)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (friendItem == null)
            {
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    friendsApi.Remove(friendItem.FriendLinkId);
                }

                Friends.Remove(friendItem);
                MessageBox.Show(Lang.friendRemovedOkText, Lang.infoTitle);
            }
            catch (Exception ex)
            {
                Logger.Error("Error removing friend.", ex);
                MessageBox.Show(Lang.errorRemovingFriendText, Lang.errorTitle);
            }
        }

        private static string TryGetLangOr(string fallback, string language)
        {
            return string.IsNullOrWhiteSpace(language) ? fallback : language;
        }
    }
}
