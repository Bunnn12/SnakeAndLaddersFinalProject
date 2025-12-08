using System.Windows;
using System.Windows.Controls;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class SignUpPage : Page
    {
        private SignUpViewModel ViewModel => DataContext as SignUpViewModel;

        public SignUpPage()
        {
            InitializeComponent();

            DataContext = new SignUpViewModel();
        }

        private async void GoSignUp(object sender, RoutedEventArgs e)
        {
            var input = new SignUpViewModel.RegistrationInput
            {
                Username = txtUsername.Text?.Trim() ?? string.Empty,
                GivenName = txtNameOfUser.Text?.Trim() ?? string.Empty,
                FamilyName = txtLastname.Text?.Trim() ?? string.Empty,
                EmailAddress = txtEmail.Text?.Trim() ?? string.Empty,
                PlainPassword = pwdPassword.Password ?? string.Empty
            };

            if (ViewModel == null)
            {
                ShowWarn(T("UiGenericError"));
                return;
            }

            SignUpViewModel.RegistrationResult result =
                await ViewModel.SignUpAsync(input);

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
            }
            else
            {
                ShowWarn(T("UiNoBackPage"));
            }
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
