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
        private string[] ValidateRegistration(string user, string name, string last, string email, string pwd)
        {
            var errs = new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(name)) errs.Add("Nombre es requerido.");
            if (string.IsNullOrWhiteSpace(last)) errs.Add("Apellidos requeridos.");
            if (string.IsNullOrWhiteSpace(user)) errs.Add("Usuario es requerido.");
            if (string.IsNullOrWhiteSpace(email)) errs.Add("Correo es requerido.");
            else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) errs.Add("Correo inválido.");
            if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 8) errs.Add("Contraseña: mínimo 8 caracteres.");
            return errs.ToArray();
        }

        // Click de "Registrar"
        private async void BtnSignUp_Click(object sender, RoutedEventArgs e)
        {
            var errs = ValidateRegistration(
                txtUsername.Text, txtNameOfUser.Text, txtLastname.Text, txtEmail.Text, pwdPassword.Password);

            if (errs.Any())
            {
                MessageBox.Show(string.Join("\n", errs), "Corrige esto", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // DTOs generados por el Service Reference "AuthService"
            var dto = new AuthService.RegistrationDto
            {
                UserName = txtUsername.Text,
                FirstName = txtNameOfUser.Text,
                LastName = txtLastname.Text,
                Email = txtEmail.Text,
                Password = pwdPassword.Password
            };

            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");
            try
            {
                // Llamada sin bloquear la UI
                var res = await Task.Run(() => client.Register(dto));

                if (res.Success)
                {
                    MessageBox.Show($"Registro correcto: {res.DisplayName}", "Registro", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService?.Navigate(new MainPage());
                    client.Close();
                    return;
                }
                else
                {
                    MessageBox.Show(res.Message, "Registro", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                client.Close();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                MessageBox.Show("Servidor no encontrado en http://localhost:8085/Auth.\n¿Está corriendo el host?",
                                "Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                client.Abort();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Registro", MessageBoxButton.OK, MessageBoxImage.Error);
                client.Abort();
            }
        }

        // Click de "Volver"
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
            else
                MessageBox.Show("No hay página anterior.");
        }
    }
}
