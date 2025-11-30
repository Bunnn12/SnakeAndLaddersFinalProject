using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.StatsService;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ProfileStatsPage : Page
    {
        private readonly bool isOwnProfile;

        private ProfileStatsViewModel ViewModel
        {
            get { return DataContext as ProfileStatsViewModel; }
        }

        public ProfileStatsPage()
        {
            InitializeComponent();

            isOwnProfile = true;
            DataContext = ProfileStatsViewModel.CreateForCurrentUser();
        }
        public ProfileStatsPage(int userId, string username, string avatarId)
        {
            InitializeComponent();

            isOwnProfile = false;
            DataContext = ProfileStatsViewModel.CreateForOtherUser(userId, username, avatarId);
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.LoadStats();
            RefreshBindings();
        }

        private void RefreshBindings()
        {
            var currentDataContext = DataContext;
            DataContext = null;
            DataContext = currentDataContext;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                return;
            }

            var navigationWindow = Application.Current.MainWindow as NavigationWindow;
            if (navigationWindow != null && navigationWindow.CanGoBack)
            {
                navigationWindow.GoBack();
                return;
            }

            Page fallbackPage = isOwnProfile
                ? (Page)new ProfilePage()
                : new MainPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(fallbackPage);
                return;
            }

            if (navigationWindow != null)
            {
                navigationWindow.Navigate(fallbackPage);
            }
            else
            {
                Application.Current.MainWindow.Content = fallbackPage;
            }
        }
    }
}
