using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class EmailVerificationPage : Page
    {
        private readonly AuthService.RegistrationDto _pendingDto;

        // Recibe el DTO completo (cambiamos el ctor)
        public EmailVerificationPage(AuthService.RegistrationDto pendingDto)
        {
            InitializeComponent();

            _pendingDto = pendingDto ?? throw new ArgumentNullException(nameof(pendingDto));

            lblVerificationMessage.Content = $"A verification code has been sent to {_pendingDto.Email}";
            btnVerificateCode.Click += BtnVerificateCode_Click;
        }

        private async void BtnVerificateCode_Click(object sender, RoutedEventArgs e)
        {
            var code = (txtCodeSended.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Please enter the verification code.", "Verification",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");
            try
            {
                // 1) Confirmar el código
                var confirm = await Task.Run(() => client.ConfirmEmailVerification(_pendingDto.Email, code));
                if (!confirm.Success)
                {
                    MessageBox.Show(confirm.Message ?? "Invalid or expired code.", "Verification",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    client.Close();
                    return;
                }

                // 2) Registrar AHORA SÍ
                var register = await Task.Run(() => client.Register(_pendingDto));
                if (!register.Success)
                {
                    MessageBox.Show(register.Message ?? "Could not create user.", "Register",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    client.Close();
                    return;
                }

                MessageBox.Show($"Account created: {register.DisplayName}", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                // Redirige a donde prefieras tras crear cuenta
                NavigationService?.Navigate(new MainPage());
                client.Close();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                MessageBox.Show("Auth host not found. Is the service running?", "Connection",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                client.Abort();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Verification",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                client.Abort();
            }
        }
    }
}
