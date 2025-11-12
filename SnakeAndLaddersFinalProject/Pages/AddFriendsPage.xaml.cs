using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class AddFriendsPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddFriendsPage));

        private readonly ObservableCollection<UserBriefDto> results = new ObservableCollection<UserBriefDto>();
        private readonly DispatcherTimer debounce;

        private const int DEBOUNCE_MS = 250;
        private const int SEARCH_MAX_RESULTS = 20;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            txtFindFriend.Focus();
            Keyboard.Focus(txtFindFriend);
            txtFindFriend.CaretIndex = txtFindFriend.Text?.Length ?? 0;
        }

        public AddFriendsPage()
        {
            InitializeComponent();
            tvSearchResults.ItemsSource = results;

            if (!SessionGuard.HasValidSession()) return;

            debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DEBOUNCE_MS) };
            debounce.Tick += (s, e) => { debounce.Stop(); RunSearch(txtFindFriend.Text); };
        }

        private void TxtFindFriend_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!SessionGuard.HasValidSession()) return;
            debounce.Stop();
            debounce.Start();
        }

        private void RunSearch(string term)
        {
            if (!SessionGuard.HasValidSession()) return;

            term = (term ?? string.Empty).Trim();
            if (term.Length < 2)
            {
                results.Clear();
                return;
            }

            try
            {
                using (var api = new FriendsApi())
                {
                    results.Clear();
                    foreach (var u in api.SearchUsers(term, SEARCH_MAX_RESULTS))
                        results.Add(u);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error searching users.", ex);
                MessageBox.Show(Lang.errorSearchingUsersText, Lang.errorTitle);
            }
        }

        private void BtnAddFriend_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession()) return;

            if (((FrameworkElement)sender).DataContext is UserBriefDto user)
            {
                try
                {
                    using (var api = new FriendsApi())
                    {
                        api.SendFriendRequest(user.UserId);
                    }
                    results.Remove(user);
                    MessageBox.Show(Lang.friendRequestSentText, Lang.infoTitle);
                }
                catch (FaultException ex)
                {
                    Logger.WarnFormat("SendFriendRequest fault: {0} - {1}", ex.Code, ex.Message);
                    MessageBox.Show(ex.Message, Lang.errorTitle);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error sending friend request.", ex);
                    MessageBox.Show(Lang.errorSendingRequestText, Lang.errorTitle);
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
