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
        private static readonly ILog _logger = LogManager.GetLogger(typeof(FriendsListViewModel));

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
                using (FriendsApi friendsApi = new FriendsApi())
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
                UiExceptionHelper.ShowModuleError(
                    ex,
                    nameof(LoadFriends),
                    _logger,
                    Lang.UiFriendsLoadError);
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

            string title = Lang.FriendUnfriendConfirmTitle;
            string message = Lang.FriendUnfriendConfirmText;

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
                using (FriendsApi friendsApi = new FriendsApi())
                {
                    friendsApi.Remove(friendItem.FriendLinkId);
                }

                Friends.Remove(friendItem);

                MessageBox.Show(
                    Lang.friendRemovedOkText,
                    Lang.infoTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UiExceptionHelper.ShowModuleError(
                    ex,
                    nameof(UnfriendWithConfirmation),
                    _logger,
                    Lang.UiFriendUnfriendError);
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
                using (FriendsApi friendsApi = new FriendsApi())
                {
                    friendsApi.Remove(friendItem.FriendLinkId);
                }

                Friends.Remove(friendItem);

                MessageBox.Show(
                    Lang.friendRemovedOkText,
                    Lang.infoTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UiExceptionHelper.ShowModuleError(
                    ex,
                    nameof(UnfriendDirect),
                    _logger,
                    Lang.UiFriendUnfriendDirectError);
            }
        }
    }
}
