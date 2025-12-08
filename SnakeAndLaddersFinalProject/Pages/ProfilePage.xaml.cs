using log4net;
using SnakeAndLaddersFinalProject.SocialProfileService;
using SnakeAndLaddersFinalProject.UserService;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.Windows;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using Lang = SnakeAndLaddersFinalProject.Properties.Langs.Lang;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ProfilePage : Page
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ProfilePage));

        private readonly SocialProfilesViewModel _socialProfilesViewModel;

        private ProfileViewModel ViewModel
        {
            get { return DataContext as ProfileViewModel; }
        }

        public ProfilePage()
        {
            InitializeComponent();

            DataContext = new ProfileViewModel();
            _socialProfilesViewModel = new SocialProfilesViewModel();
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            bool loaded = viewModel.LoadProfile();
            if (loaded && viewModel.LoadedAccount != null)
            {
                ApplyLoadedAccountToUi(viewModel.LoadedAccount);

                InitializeSocialProfiles(viewModel.LoadedAccount.UserId);

                bool avatarsLoaded = viewModel.LoadAvatarOptions();
                if (!avatarsLoaded)
                {
                    _logger.Warn("Avatar options could not be loaded.");
                }

                RefreshAvatarBinding();
            }

            SetEditMode(false);
        }

        private void ApplyLoadedAccountToUi(AccountDto account)
        {
            if (account == null)
            {
                return;
            }

            txtUsername.Text = account.Username;
            txtFirstName.Text = account.FirstName;
            txtLastName.Text = account.LastName;
            txtDescription.Text = account.ProfileDescription;
            txtCoins.Text = account.Coins.ToString();
        }

        private void SetEditMode(bool enabled)
        {
            txtFirstName.IsReadOnly = !enabled;
            txtLastName.IsReadOnly = !enabled;
            txtDescription.IsReadOnly = !enabled;

            txtCoins.IsReadOnly = true;

            btnGuardar.IsEnabled = enabled;
            btnCancelar.IsEnabled = enabled;

            borderEditBanner.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void EditProfile(object sender, RoutedEventArgs e)
        {
            SetEditMode(true);
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel?.LoadedAccount != null)
            {
                ApplyLoadedAccountToUi(viewModel.LoadedAccount);
                RefreshAvatarBinding();
            }

            SetEditMode(false);
        }

        private void SaveChanges(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel?.LoadedAccount == null)
            {
                return;
            }

            string firstName = txtFirstName.Text?.Trim();
            string lastName = txtLastName.Text?.Trim();
            string description = txtDescription.Text?.Trim();

            if (!viewModel.ValidateProfileInputs(firstName, lastName, description))
            {
                return;
            }

            bool updated = viewModel.TryUpdateProfile(firstName, lastName, description);
            if (!updated || viewModel.LoadedAccount == null)
            {
                return;
            }

            ApplyLoadedAccountToUi(viewModel.LoadedAccount);
            RefreshAvatarBinding();
            SetEditMode(false);
        }

        private void MenuClick(object sender, RoutedEventArgs e)
        {
            if (btnMenu.ContextMenu != null)
            {
                btnMenu.ContextMenu.PlacementTarget = btnMenu;
                btnMenu.ContextMenu.IsOpen = true;
            }
        }

        private void MenuDeleteAccount(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel?.LoadedAccount == null)
            {
                MessageBox.Show(
                    Lang.ProfileAccountInfoLoadError,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                Lang.ProfileDeactivateConfirmText,
                Lang.ProfileDeactivateConfirmTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            viewModel.TryDeactivateAccount();
        }

        private void MenuViewStats(object sender, RoutedEventArgs e)
        {
            var statsPage = new ProfileStatsPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(statsPage);
                return;
            }

            var navigationWindow = Application.Current.MainWindow as NavigationWindow;
            if (navigationWindow != null)
            {
                navigationWindow.Navigate(statsPage);
            }
            else
            {
                Application.Current.MainWindow.Content = statsPage;
            }
        }

        private void ChangeAvatar(object sender, RoutedEventArgs e)
        {
            borderAvatarPicker.Visibility =
                borderAvatarPicker.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private void CloseAvatarPicker(object sender, RoutedEventArgs e)
        {
            borderAvatarPicker.Visibility = Visibility.Collapsed;
        }

        private void AvatarItem(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel?.LoadedAccount == null)
            {
                MessageBox.Show(
                    Lang.ProfileNotLoadedError,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            var option = button.Tag as AvatarProfileOptionViewModel;
            if (option == null || !option.IsUnlocked)
            {
                return;
            }

            string avatarId = option.AvatarCode;
            if (string.IsNullOrWhiteSpace(avatarId))
            {
                return;
            }

            var result = MessageBox.Show(
                Lang.ProfileChangeAvatarConfirmText,
                Lang.ProfileChangeAvatarConfirmTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            bool updated = viewModel.TryUpdateAvatar(avatarId);
            if (!updated || viewModel.LoadedAccount == null)
            {
                return;
            }

            ApplyLoadedAccountToUi(viewModel.LoadedAccount);
            RefreshAvatarBinding();

            borderAvatarPicker.Visibility = Visibility.Collapsed;
        }

        private void Back(object sender, RoutedEventArgs e)
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

            var mainPage = new MainPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(mainPage);
                return;
            }

            if (navigationWindow != null)
            {
                navigationWindow.Navigate(mainPage);
            }
            else
            {
                Application.Current.MainWindow.Content = mainPage;
            }
        }

        private void RefreshAvatarBinding()
        {
            var currentDataContext = DataContext;
            DataContext = null;
            DataContext = currentDataContext;
        }

        private void InitializeSocialProfiles(int userId)
        {
            _socialProfilesViewModel.LoadSocialProfiles(userId);
            ApplySocialProfilesToUi();
        }

        private void ApplySocialProfilesToUi()
        {
            ApplySocialProfileToHyperlink(
                _socialProfilesViewModel.Instagram,
                lnkInstagramProfile,
                Lang.ProfileNetworkInstagramText);

            ApplySocialProfileToHyperlink(
                _socialProfilesViewModel.Facebook,
                lnkFacebookProfile,
                Lang.ProfileNetworkFacebookText);

            ApplySocialProfileToHyperlink(
                _socialProfilesViewModel.Twitter,
                lnkTwitterProfile,
                Lang.ProfileNetworkTwitterText);

            UpdateSocialMenuHeaders();
        }

        private static void ApplySocialProfileToHyperlink(
            SocialProfileItemViewModel item,
            Hyperlink hyperlink,
            string displayName)
        {
            if (item == null || hyperlink == null)
            {
                return;
            }

            hyperlink.Inlines.Clear();

            if (item.IsLinked)
            {
                hyperlink.Inlines.Add(displayName);
                hyperlink.IsEnabled = true;
            }
            else
            {
                hyperlink.Inlines.Add(Lang.ProfileSocialNotLinkedText);
                hyperlink.IsEnabled = false;
            }
        }

        private void UpdateSocialMenuHeaders()
        {
            UpdateMenuHeader(
                miInstagramLinkToggle,
                _socialProfilesViewModel.Instagram,
                Lang.ProfileNetworkInstagramText);

            UpdateMenuHeader(
                miFacebookLinkToggle,
                _socialProfilesViewModel.Facebook,
                Lang.ProfileNetworkFacebookText);

            UpdateMenuHeader(
                miTwitterLinkToggle,
                _socialProfilesViewModel.Twitter,
                Lang.ProfileNetworkTwitterText);
        }

        private static void UpdateMenuHeader(
            MenuItem menuItem,
            SocialProfileItemViewModel item,
            string networkDisplayName)
        {
            if (menuItem == null || item == null)
            {
                return;
            }

            menuItem.Header = item.IsLinked
                ? string.Format(Lang.ProfileMenuUnlinkFmt, networkDisplayName)
                : string.Format(Lang.ProfileMenuLinkFmt, networkDisplayName);
        }

        private void LnkInstagramProfile(object sender, RoutedEventArgs e)
        {
            _socialProfilesViewModel.TryOpenSavedProfile(SocialNetworkType.Instagram);
        }

        private void LnkFacebookProfile(object sender, RoutedEventArgs e)
        {
            _socialProfilesViewModel.TryOpenSavedProfile(SocialNetworkType.Facebook);
        }

        private void LnkTwitterProfile(object sender, RoutedEventArgs e)
        {
            _socialProfilesViewModel.TryOpenSavedProfile(SocialNetworkType.Twitter);
        }

        private void MenuInstagramLinkToggle(object sender, RoutedEventArgs e)
        {
            ToggleSocialLink(SocialNetworkType.Instagram, _socialProfilesViewModel.Instagram);
        }

        private void MenuFacebookLinkToggle(object sender, RoutedEventArgs e)
        {
            ToggleSocialLink(SocialNetworkType.Facebook, _socialProfilesViewModel.Facebook);
        }

        private void MenuTwitterLinkToggle(object sender, RoutedEventArgs e)
        {
            ToggleSocialLink(SocialNetworkType.Twitter, _socialProfilesViewModel.Twitter);
        }

        private async void Logout(object sender, RoutedEventArgs e)
        {
            try
            {
                await AuthClientHelper.LogoutAsync().ConfigureAwait(true);

                var loginPage = new LoginPage();

                if (NavigationService != null)
                {
                    NavigationService.Navigate(loginPage);
                    return;
                }

                var navigationWindow = Application.Current.MainWindow as NavigationWindow;
                if (navigationWindow != null)
                {
                    navigationWindow.Navigate(loginPage);
                }
                else
                {
                    Application.Current.MainWindow.Content = loginPage;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error while logging out from ProfilePage.", ex);

                MessageBox.Show(
                    Lang.UiGenericError,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ToggleSocialLink(SocialNetworkType network, SocialProfileItemViewModel item)
        {
            var profileVm = ViewModel;
            if (profileVm?.LoadedAccount == null)
            {
                MessageBox.Show(
                    Lang.ProfileUserNotLoadedError,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            int userId = profileVm.LoadedAccount.UserId;

            if (item != null && item.IsLinked)
            {
                var confirm = MessageBox.Show(
                    Lang.SocialProfileUnlinkConfirmText,
                    Lang.SocialProfileUnlinkConfirmTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                bool unlinked = _socialProfilesViewModel.TryUnlinkProfile(userId, network);
                if (!unlinked)
                {
                    return;
                }
            }
            else
            {
                _socialProfilesViewModel.TryOpenNetworkHome(network);

                var dialog = new SocialProfileLinkWindow(network)
                {
                    Owner = Application.Current.MainWindow
                };

                bool? result = dialog.ShowDialog();
                if (result != true)
                {
                    return;
                }

                string profileLink = dialog.ProfileLink;

                bool linked = _socialProfilesViewModel.TryLinkProfile(userId, network, profileLink);
                if (!linked)
                {
                    return;
                }
            }
            _socialProfilesViewModel.LoadSocialProfiles(userId);
            ApplySocialProfilesToUi();
        }
    }
}
