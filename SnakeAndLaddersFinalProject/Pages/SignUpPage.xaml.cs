using System.Windows;
using System.Windows.Controls;
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

        private async void SignUp(object sender, RoutedEventArgs e)
        {
            var input = new SignUpViewModel.RegistrationInput
            {
                Username = txtUsername.Text,
                GivenName = txtNameOfUser.Text,
                FamilyName = txtLastname.Text,
                EmailAddress = txtEmail.Text,
                PlainPassword = pwdPassword.Password
            };

            var registrationDto = await ViewModel.SignUpAsync(input);

            if (registrationDto == null)
            {
                return;
            }

            NavigationService?.Navigate(new EmailVerificationPage(registrationDto));
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
