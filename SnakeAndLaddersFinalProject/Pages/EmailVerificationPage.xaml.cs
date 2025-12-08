using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.AuthService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class EmailVerificationPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EmailVerificationPage));

        private const int DEFAULT_RESEND_COOLDOWN_SECONDS = 50;
        private const int MIN_RESEND_SECONDS = 1;
        private const int VERIFICATION_CODE_LENGTH = 6;


        private DispatcherTimer _resendTimer;
        private int _remainingSeconds;

        private EmailVerificationViewModel ViewModel =>
            DataContext as EmailVerificationViewModel;

        public EmailVerificationPage()
            : this(new RegistrationDto
            {
                Email = string.Empty,
                FirstName = string.Empty,
                LastName = string.Empty,
                Password = string.Empty,
                UserName = string.Empty
            })
        {
        }

        public EmailVerificationPage(RegistrationDto pendingDto)
        {
            InitializeComponent();

            var viewModel = new EmailVerificationViewModel(pendingDto);
            DataContext = viewModel;

            viewModel.ResendCooldownRequested += OnResendCooldownRequested;
            viewModel.NavigateToLoginRequested += OnNavigateToLoginRequested;
            btnVerificateCode.Click += VerifyCode;
            btnResendCode.Click += ResendCode;
            btnBack.Click += Back;

            StartResendCooldown(DEFAULT_RESEND_COOLDOWN_SECONDS);
        }

        private void Back(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new LoginPage());
        }

        private async void VerifyCode(object sender, RoutedEventArgs e)
        {
            string code = (txtCodeSended.Text ?? string.Empty).Trim();

            if (!IsValidVerificationCode(code))
            {
                MessageBox.Show(
                    Lang.AuthCodeInvalid,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            await viewModel.VerificateCodeAsync(code);
        }

        private async void ResendCode(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            await viewModel.ResendCodeAsync();
        }

        private void OnResendCooldownRequested(int seconds)
        {
            StartResendCooldown(seconds);
        }

        private void OnNavigateToLoginRequested()
        {
            try
            {
                btnVerificateCode.IsEnabled = false;
                NavigationService?.Navigate(new LoginPage());
            }
            catch (Exception ex)
            {
                Logger.Error("Error while navigating to Login from EmailVerificationPage.", ex);

                MessageBox.Show(
                    Lang.UiUnexpectedNavigationError,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void StartResendCooldown(int seconds)
        {
            _remainingSeconds = Math.Max(MIN_RESEND_SECONDS, seconds);

            btnResendCode.IsEnabled = false;
            UpdateResendButtonContent();

            if (_resendTimer == null)
            {
                _resendTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _resendTimer.Tick += ResendTimerTick;
            }

            _resendTimer.Start();
        }

        private void ResendTimerTick(object sender, EventArgs e)
        {
            _remainingSeconds--;

            if (_remainingSeconds <= 0)
            {
                _resendTimer.Stop();
                btnResendCode.IsEnabled = true;
                btnResendCode.Content = Lang.btnResendCodeText; 
            }
            else
            {
                UpdateResendButtonContent();
            }
        }

        private void UpdateResendButtonContent()
        {
            btnResendCode.Content = string.Format(
                Lang.UiResendCodeInFmt,   
                _remainingSeconds);
        }

        private static bool IsValidVerificationCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            if (code.Length != VERIFICATION_CODE_LENGTH)
            {
                return false;
            }

            for (int index = 0; index < code.Length; index++)
            {
                if (!char.IsDigit(code[index]))
                {
                    return false;
                }
            }

            return true;
        }

    }
}
