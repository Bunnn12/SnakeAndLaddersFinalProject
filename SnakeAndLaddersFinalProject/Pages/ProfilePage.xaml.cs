using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.SocialProfileService;
using SnakeAndLaddersFinalProject.UserService;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.Windows;
using Lang = SnakeAndLaddersFinalProject.Properties.Langs.Lang;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ProfilePage : Page
    {
        private const string INSTAGRAM_URL = "https://www.instagram.com/";
        private const string FACEBOOK_URL = "https://www.facebook.com/";
        private const string TWITTER_URL = "https://_x.com/";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProfilePage));

        private readonly SocialProfilesViewModel socialProfilesViewModel;

        private ProfileViewModel ViewModel
        {
            get { return DataContext as ProfileViewModel; }
        }

        public ProfilePage()
        {
            InitializeComponent();

            DataContext = new ProfileViewModel();
            socialProfilesViewModel = new SocialProfilesViewModel();
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
                    Logger.Warn("Avatar options could not be loaded.");
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

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode(true);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel?.LoadedAccount != null)
            {
                ApplyLoadedAccountToUi(viewModel.LoadedAccount);
                RefreshAvatarBinding();
            }

            SetEditMode(false);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
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

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            if (btnMenu.ContextMenu != null)
            {
                btnMenu.ContextMenu.PlacementTarget = btnMenu;
                btnMenu.ContextMenu.IsOpen = true;
            }
        }

        private void MenuDeleteAccount_Click(object sender, RoutedEventArgs e)
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

        private void MenuViewStats_Click(object sender, RoutedEventArgs e)
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

        private void BtnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            borderAvatarPicker.Visibility =
                borderAvatarPicker.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private void BtnCloseAvatarPicker_Click(object sender, RoutedEventArgs e)
        {
            borderAvatarPicker.Visibility = Visibility.Collapsed;
        }

        private void AvatarItem_Click(object sender, RoutedEventArgs e)
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
            socialProfilesViewModel.LoadSocialProfiles(userId);
            ApplySocialProfilesToUi();
        }

        private void ApplySocialProfilesToUi()
        {
            ApplySocialProfileToHyperlink(
                socialProfilesViewModel.Instagram,
                lnkInstagramProfile,
                Lang.ProfileNetworkInstagramText);

            ApplySocialProfileToHyperlink(
                socialProfilesViewModel.Facebook,
                lnkFacebookProfile,
                Lang.ProfileNetworkFacebookText);

            ApplySocialProfileToHyperlink(
                socialProfilesViewModel.Twitter,
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
                socialProfilesViewModel.Instagram,
                Lang.ProfileNetworkInstagramText);

            UpdateMenuHeader(
                miFacebookLinkToggle,
                socialProfilesViewModel.Facebook,
                Lang.ProfileNetworkFacebookText);

            UpdateMenuHeader(
                miTwitterLinkToggle,
                socialProfilesViewModel.Twitter,
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

        private void OpenNetworkInBrowser(SocialNetworkType network)
        {
            string url;

            switch (network)
            {
                case SocialNetworkType.Instagram:
                    url = INSTAGRAM_URL;
                    break;
                case SocialNetworkType.Facebook:
                    url = FACEBOOK_URL;
                    break;
                case SocialNetworkType.Twitter:
                    url = TWITTER_URL;
                    break;
                default:
                    return;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Logger.Error("Error opening browser for social network.", ex);
                MessageBox.Show(
                    Lang.SocialBrowserOpenError,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenSavedProfile(SocialNetworkType network)
        {
            SocialProfileItemViewModel item = null;

            switch (network)
            {
                case SocialNetworkType.Instagram:
                    item = socialProfilesViewModel.Instagram;
                    break;
                case SocialNetworkType.Facebook:
                    item = socialProfilesViewModel.Facebook;
                    break;
                case SocialNetworkType.Twitter:
                    item = socialProfilesViewModel.Twitter;
                    break;
            }

            if (item == null || !item.IsLinked || string.IsNullOrWhiteSpace(item.ProfileLink))
            {
                MessageBox.Show(
                    Lang.SocialNetworkNotLinkedInfo,
                    Lang.UiTitleInfo,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = item.ProfileLink,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Logger.Error("Error opening social profile link.", ex);
                MessageBox.Show(
                    Lang.SocialProfileOpenError,
                    Lang.UiTitleError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LinkSocialProfile(SocialNetworkType network)
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

            OpenNetworkInBrowser(network);

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

            bool linked = socialProfilesViewModel.TryLinkProfile(userId, network, profileLink);
            if (!linked)
            {
                return;
            }

            socialProfilesViewModel.LoadSocialProfiles(userId);
            ApplySocialProfilesToUi();
        }

        private void UnlinkSocialProfile(SocialNetworkType network)
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

            var confirm = MessageBox.Show(
                Lang.SocialProfileUnlinkConfirmText,
                Lang.SocialProfileUnlinkConfirmTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            bool unlinked = socialProfilesViewModel.TryUnlinkProfile(userId, network);
            if (!unlinked)
            {
                return;
            }

            socialProfilesViewModel.LoadSocialProfiles(userId);
            ApplySocialProfilesToUi();
        }

        private void LnkInstagramProfile_Click(object sender, RoutedEventArgs e)
        {
            OpenSavedProfile(SocialNetworkType.Instagram);
        }

        private void LnkFacebookProfile_Click(object sender, RoutedEventArgs e)
        {
            OpenSavedProfile(SocialNetworkType.Facebook);
        }

        private void LnkTwitterProfile_Click(object sender, RoutedEventArgs e)
        {
            OpenSavedProfile(SocialNetworkType.Twitter);
        }

        private void MenuInstagramLinkToggle_Click(object sender, RoutedEventArgs e)
        {
            var item = socialProfilesViewModel.Instagram;

            if (item != null && item.IsLinked)
            {
                UnlinkSocialProfile(SocialNetworkType.Instagram);
            }
            else
            {
                LinkSocialProfile(SocialNetworkType.Instagram);
            }
        }

        private void MenuFacebookLinkToggle_Click(object sender, RoutedEventArgs e)
        {
            var item = socialProfilesViewModel.Facebook;

            if (item != null && item.IsLinked)
            {
                UnlinkSocialProfile(SocialNetworkType.Facebook);
            }
            else
            {
                LinkSocialProfile(SocialNetworkType.Facebook);
            }
        }

        private void MenuTwitterLinkToggle_Click(object sender, RoutedEventArgs e)
        {
            var item = socialProfilesViewModel.Twitter;

            if (item != null && item.IsLinked)
            {
                UnlinkSocialProfile(SocialNetworkType.Twitter);
            }
            else
            {
                LinkSocialProfile(SocialNetworkType.Twitter);
            }
        }
    }
}
