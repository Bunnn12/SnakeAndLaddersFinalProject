using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class SignUpPage : Page
    {
        public SignUpPage()
        {
            InitializeComponent();
        }

        private string[] ValidateRegistration(string userName, string firstName, string lastName, string email, string password)
        {
            var errors = new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(firstName)) errors.Add("Nombre es requerido.");
            if (string.IsNullOrWhiteSpace(lastName)) errors.Add("Apellidos requeridos.");
            if (string.IsNullOrWhiteSpace(userName)) errors.Add("Usuario es requerido.");
            if (string.IsNullOrWhiteSpace(email)) errors.Add("Correo es requerido.");
            else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) errors.Add("Correo inválido.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) errors.Add("Contraseña: mínimo 8 caracteres.");
            return errors.ToArray();
        }

        // Click: Registrar (pero SOLO pedir código y navegar)
        private async void BtnSignUp_Click(object sender, RoutedEventArgs e)
        {
            var errors = ValidateRegistration(
                txtUsername.Text, txtNameOfUser.Text, txtLastname.Text, txtEmail.Text, pwdPassword.Password);

            if (errors.Any())
            {
                MessageBox.Show(string.Join("\n", errors), "Corrige esto", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dto = new AuthService.RegistrationDto
            {
                UserName = txtUsername.Text.Trim(),
                FirstName = txtNameOfUser.Text.Trim(),
                LastName = txtLastname.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Password = pwdPassword.Password
            };

            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");
            try
            {
                // 1) Solicitar envío de código (NO registrar aún)
                var send = await Task.Run(() => client.RequestEmailVerification(dto.Email));

                if (!send.Success)
                {
                    MessageBox.Show(send.Message ?? "No fue posible enviar el código de verificación.", "Verificación de correo",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    client.Close();
                    return;
                }

                // (En DEBUG quizá regrese el código en Message)
                if (!string.IsNullOrWhiteSpace(send.Message) && send.Message.Contains("DEBUG"))
                    MessageBox.Show(send.Message, "Solo para pruebas", MessageBoxButton.OK, MessageBoxImage.Information);

                // 2) Navegar a verificación LLEVANDO EL DTO COMPLETO
                NavigationService?.Navigate(new EmailVerificationPage(dto));
                client.Close();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                MessageBox.Show("No se pudo solicitar el código. ¿Está corriendo el host? (http://localhost:8085/Auth)",
                                "Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                client.Abort();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Registro", MessageBoxButton.OK, MessageBoxImage.Error);
                client.Abort();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
            else MessageBox.Show("No hay página anterior.");
        }
    }
}
