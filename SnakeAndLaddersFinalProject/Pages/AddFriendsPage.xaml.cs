using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.FriendsService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class AddFriendsPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddFriendsPage));

        private const int DEBOUNCE_MS = 250;

        private readonly DispatcherTimer _searchDebounceTimer;

        private AddFriendsViewModel ViewModel
        {
            get { return DataContext as AddFriendsViewModel; }
        }

        public AddFriendsPage()
        {
            InitializeComponent();

            DataContext = new AddFriendsViewModel(Logger);

            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (ViewModel != null)
            {
                tvSearchResults.ItemsSource = ViewModel.SearchResults;
            }

            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(DEBOUNCE_MS)
            };

            _searchDebounceTimer.Tick += (_, __) =>
            {
                _searchDebounceTimer.Stop();
                RunSearch(txtFindFriend.Text);
            };
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            txtFindFriend.Focus();
            Keyboard.Focus(txtFindFriend);
            txtFindFriend.CaretIndex = txtFindFriend.Text?.Length ?? 0;
        }

        private void FindFriendTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void RunSearch(string term)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            vm.RunSearch(term);
        }

        private void AddFriend(object sender, RoutedEventArgs e)
        {
            if (!SessionGuard.HasValidSession())
            {
                return;
            }

            if (!(((FrameworkElement)sender).DataContext is UserBriefDto user))
            {
                return;
            }

            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            vm.AddFriend(user);
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
