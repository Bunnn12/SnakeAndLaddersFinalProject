using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private readonly ILog logger;

        public ObservableCollection<UserBriefDto> SearchResults { get; }

        public AddFriendsViewModel(ILog logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SearchResults = new ObservableCollection<UserBriefDto>();
        }

        public void RunSearch(string term)
        {
            term = (term ?? string.Empty).Trim();

            if (term.Length < MIN_SEARCH_TERM_LENGTH)
            {
                SearchResults.Clear();
                return;
            }

            try
            {
                using (var friendsApi = new FriendsApi())
                {
                    SearchResults.Clear();

                    foreach (UserBriefDto user in friendsApi.SearchUsers(term, SEARCH_MAX_RESULTS))
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
                    logger);

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
                using (var friendsApi = new FriendsApi())
                {
                    FriendLinkDto link = friendsApi.SendFriendRequest(user.UserId);
                    SearchResults.Remove(user);

                    if (link != null && link.Status == FriendRequestStatus.Accepted)
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
                    logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
