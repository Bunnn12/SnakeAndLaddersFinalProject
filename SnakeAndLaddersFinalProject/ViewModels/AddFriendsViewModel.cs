using System;
using System.Collections.ObjectModel;
using log4net;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using System.Windows;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class AddFriendsViewModel
    {
        private const int SEARCH_MAX_RESULTS = 20;
        private const int MIN_SEARCH_TERM_LENGTH = 2;

        private readonly ILog _logger;

        public ObservableCollection<UserBriefDto> SearchResults { get; }

        public AddFriendsViewModel(ILog logger)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SearchResults = new ObservableCollection<UserBriefDto>();
        }

        public void RunSearch(string searchTerm)
        {
            searchTerm = (searchTerm ?? string.Empty).Trim();

            if (searchTerm.Length < MIN_SEARCH_TERM_LENGTH)
            {
                SearchResults.Clear();
                return;
            }

            try
            {
                using (FriendsApi friendsApi = new FriendsApi())
                {
                    SearchResults.Clear();

                    foreach (UserBriefDto user in friendsApi.SearchUsers(searchTerm, SEARCH_MAX_RESULTS))
                    {
                        SearchResults.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(AddFriendsViewModel)}.{nameof(RunSearch)}",
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void AddFriend(UserBriefDto user)
        {
            if (user == null)
            {
                return;
            }

            try
            {
                using (FriendsApi friendsApi = new FriendsApi())
                {
                    FriendLinkDto friendLink = friendsApi.SendFriendRequest(user.UserId);
                    SearchResults.Remove(user);

                    if (friendLink != null && friendLink.Status == FriendRequestStatus.Accepted)
                    {
                        MessageBox.Show(
                            Lang.autoAcceptedFriendText,
                            Lang.infoTitle);
                    }
                    else
                    {
                        MessageBox.Show(
                            Lang.friendRequestSentText,
                            Lang.infoTitle);
                    }
                }
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(AddFriendsViewModel)}.{nameof(AddFriend)}",
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
