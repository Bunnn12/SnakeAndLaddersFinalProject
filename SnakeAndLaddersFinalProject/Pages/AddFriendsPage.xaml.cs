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
        private const int DEBOUNCE_MILLISECONDS = 250;
        private const int DEFAULT_CARET_INDEX = 0;

        private static readonly ILog _logger = LogManager.GetLogger(typeof(AddFriendsPage));

        private readonly DispatcherTimer _searchDebounceTimer;

        private AddFriendsViewModel ViewModel
        {
            get { return DataContext as AddFriendsViewModel; }
        }

        public AddFriendsPage()
        {
            InitializeComponent();

            DataContext = new AddFriendsViewModel(_logger);

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
                Interval = TimeSpan.FromMilliseconds(DEBOUNCE_MILLISECONDS)
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
            txtFindFriend.CaretIndex = txtFindFriend.Text?.Length ?? DEFAULT_CARET_INDEX;
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

            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.RunSearch(term);
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

            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.AddFriend(user);
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
