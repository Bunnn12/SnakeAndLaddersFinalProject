using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class SignUpPage : Page
    {
        private SignUpViewModel ViewModel
        {
            get { return DataContext as SignUpViewModel; }
        }

        public SignUpPage()
        {
            InitializeComponent();
            DataContext = new SignUpViewModel();
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            if (btnNavLogin != null)
            {
                btnNavLogin.IsChecked = false;
                btnNavLogin.Content = Lang.btnLoginText;
            }

            if (btnNavSignUp != null)
            {
                btnNavSignUp.IsChecked = true;
                btnNavSignUp.Content = Lang.btnSignUpText;
            }

            if (btnSignUp != null)
            {
                btnSignUp.Content = Lang.btnSignUpText;
            }

            if (lblGivenName != null)
            {
                lblGivenName.Content = Lang.txtNameOfUserText;
            }

            if (lblFamilyName != null)
            {
                lblFamilyName.Content = Lang.txtLastNameOfUserText;
            }

            if (lblUsername != null)
            {
                lblUsername.Content = Lang.txtUsernameText;
            }

            if (lblPassword != null)
            {
                lblPassword.Content = Lang.pwdPasswordText;
            }

            if (lblEmail != null)
            {
                lblEmail.Content = Lang.txtRegEmailText;
            }
        }

        private async void GoSignUp(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                ShowWarn(T("UiGenericError"));
                return;
            }

            var input = new SignUpViewModel.RegistrationInput
            {
                Username = txtUsername.Text?.Trim() ?? string.Empty,
                GivenName = txtNameOfUser.Text?.Trim() ?? string.Empty,
                FamilyName = txtLastname.Text?.Trim() ?? string.Empty,
                EmailAddress = txtEmail.Text?.Trim() ?? string.Empty,
                PlainPassword = pwdPassword.Password ?? string.Empty
            };

            SignUpViewModel.RegistrationResult result = await viewModel.SignUpAsync(input);

            if (!result.IsSuccess || result.Registration == null)
            {
                return;
            }

            NavigationService?.Navigate(new EmailVerificationPage(result.Registration));
        }

        private void Login(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
                return;
            }

            NavigationService?.Navigate(new LoginPage());
        }

        private void Settings(object sender, RoutedEventArgs e)
        {
            if (TryGetMainFrame(out Frame mainFrame))
            {
                mainFrame.Navigate(new SettingsPage());
                return;
            }

            NavigationService?.Navigate(new SettingsPage());
        }

        private bool TryGetMainFrame(out Frame mainFrame)
        {
            Window owner = Window.GetWindow(this) ?? Application.Current?.MainWindow;
            mainFrame = owner?.FindName("MainFrame") as Frame;
            return mainFrame != null;
        }

        private static string T(string key)
        {
            return Globalization.LocalizationManager.Current[key];
        }

        private static void ShowWarn(string message)
        {
            MessageBox.Show(
                message,
                T("UiTitleWarning"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
