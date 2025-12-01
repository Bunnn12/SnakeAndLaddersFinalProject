using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ChangePasswordPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChangePasswordPage));

        private const string UI_NO_BACK_PAGE = "UiNoBackPage";
        private const string UI_TITLE_WARNING = "UiTitleWarning";

        private ChangePasswordViewModel ViewModel
        {
            get { return DataContext as ChangePasswordViewModel; }
        }

        public ChangePasswordPage()
        {
            InitializeComponent();

            DataContext = new ChangePasswordViewModel();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            vm.PasswordChangedSuccessfully -= OnPasswordChangedSuccessfully;
            vm.PasswordChangedSuccessfully += OnPasswordChangedSuccessfully;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            vm.PasswordChangedSuccessfully -= OnPasswordChangedSuccessfully;
        }

        private async void BtnSendCode_Click(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            vm.Email = (txtEmail.Text ?? string.Empty).Trim();

            await vm.SendCodeAsync();
        }

        private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            vm.Email = (txtEmail.Text ?? string.Empty).Trim();
            vm.VerificationCode = (txtVerificationCode.Text ?? string.Empty).Trim();
            vm.NewPassword = (pwdNewPassword.Password ?? string.Empty).Trim();
            vm.ConfirmPassword = (pwdConfirmPassword.Password ?? string.Empty).Trim();

            await vm.ChangePasswordAsync();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
                return;
            }

            MessageBox.Show(
                Globalization.LocalizationManager.Current[UI_NO_BACK_PAGE],
                Globalization.LocalizationManager.Current[UI_TITLE_WARNING],
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void TxtVerificationCode_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsAllDigits(e.Text))
            {
                e.Handled = true;
            }
        }

        private void TxtVerificationCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            string text = textBox.Text ?? string.Empty;

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string filtered = new string(text.Where(char.IsDigit).ToArray());

            if (filtered.Length > 6)
            {
                filtered = filtered.Substring(0, 6);
            }

            if (!string.Equals(text, filtered, StringComparison.Ordinal))
            {
                int caretIndex = textBox.CaretIndex;
                textBox.Text = filtered;
                textBox.CaretIndex = Math.Min(caretIndex, filtered.Length);
            }
        }

        private void TxtVerificationCode_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true))
            {
                e.CancelCommand();
                return;
            }

            string pastedText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;

            if (string.IsNullOrWhiteSpace(pastedText))
            {
                e.CancelCommand();
                return;
            }

            for (int index = 0; index < pastedText.Length; index++)
            {
                if (!char.IsDigit(pastedText[index]))
                {
                    e.CancelCommand();
                    return;
                }
            }
        }

        private static bool IsAllDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (int index = 0; index < text.Length; index++)
            {
                if (!char.IsDigit(text[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private void OnPasswordChangedSuccessfully()
        {
            try
            {
                txtEmail.Text = string.Empty;
                pwdNewPassword.Password = string.Empty;
                pwdConfirmPassword.Password = string.Empty;
                txtVerificationCode.Text = string.Empty;

                NavigateToLogin();
            }
            catch (Exception ex)
            {
                Logger.Error("Error al navegar al Login después de cambiar la contraseña.", ex);
            }
        }

        private void NavigateToLogin()
        {
            Window owner = Window.GetWindow(this) ?? Application.Current?.MainWindow;
            Frame mainFrame = owner?.FindName("MainFrame") as Frame;

            if (mainFrame != null)
            {
                mainFrame.Navigate(new LoginPage());
                return;
            }

            NavigationService?.Navigate(new LoginPage());
        }
    }
}
