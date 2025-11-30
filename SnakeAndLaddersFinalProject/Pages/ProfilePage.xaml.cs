using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.UserService;
using SnakeAndLaddersFinalProject.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ProfilePage : Page
    {
        private const string AVATAR_ID_PREFIX = "A";
        private const int AVATAR_FIRST_INDEX = 1;
        private const int AVATAR_LAST_INDEX = 23;

        private ProfileViewModel ViewModel
        {
            get { return DataContext as ProfileViewModel; }
        }

        public ProfilePage()
        {
            InitializeComponent();

            DataContext = new ProfileViewModel();

            InitializeAvatarOptions();
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
                RefreshAvatarBinding();
            }

            SetEditMode(false);
        }

        private void ApplyLoadedAccountToUi(AccountDto account)
        {
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

        private void MenuEditAccount_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode(true);
        }

        private void MenuDeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel?.LoadedAccount == null)
            {
                MessageBox.Show(
                    "No se pudo obtener la información de la cuenta.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                "¿Seguro que deseas desactivar tu cuenta?\n" +
                "No podrás usarla nuevamente hasta que un administrador la reactive.",
                "Desactivar cuenta",
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
            if (borderAvatarPicker.Visibility == Visibility.Visible)
            {
                borderAvatarPicker.Visibility = Visibility.Collapsed;
            }
            else
            {
                borderAvatarPicker.Visibility = Visibility.Visible;
            }
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
                    "No se ha cargado el perfil.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            string avatarId = button.Tag as string;
            if (string.IsNullOrWhiteSpace(avatarId))
            {
                return;
            }

            var result = MessageBox.Show(
                "¿Quieres usar este avatar como foto de perfil?",
                "Cambiar avatar",
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

        private void InitializeAvatarOptions()
        {
            var avatarIds = new List<string>();

            for (int index = AVATAR_FIRST_INDEX; index <= AVATAR_LAST_INDEX; index++)
            {
                string id = string.Format("{0}{1:D4}", AVATAR_ID_PREFIX, index);
                avatarIds.Add(id);
            }

            avatarItemsControl.ItemsSource = avatarIds;
        }

        private void RefreshAvatarBinding()
        {
            var currentDataContext = DataContext;
            DataContext = null;
            DataContext = currentDataContext;
        }
    }
}
