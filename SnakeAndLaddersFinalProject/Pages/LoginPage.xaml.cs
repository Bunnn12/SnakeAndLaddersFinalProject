using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SnakeAndLaddersFinalProject.Pages; // para navegar a SignUpPage/MainPage

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        // Validación local (usamos el textbox como CORREO)
        private string[] ValidateLogin(string identifier, string pwd)
        {
            var errs = new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(identifier))
                errs.Add("Usuario o correo es requerido.");
            else if (identifier.Contains("@") &&
                     !Regex.IsMatch(identifier, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errs.Add("Correo inválido.");
            if (string.IsNullOrWhiteSpace(pwd))
                errs.Add("Contraseña es requerida.");
            return errs.ToArray();
        }


        // Enter en el PasswordBox = intentar login
        private void PwdPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnLogin_Click(btnLogin, new RoutedEventArgs());
        }

        // Ir a SignUpPage
        private void BtnSignUp_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SignUpPage());
        }

        // Login
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var id = txtUsername.Text.Trim();   // usuario O correo
            var pwd = pwdPassword.Password;

            var errs = ValidateLogin(id, pwd);
            if (errs.Any()) { MessageBox.Show(string.Join("\n", errs), "Corrige esto"); return; }

            var dto = new AuthService.LoginDto { Email = id, Password = pwd }; // 'Email' = identificador
            var client = new AuthService.AuthServiceClient("BasicHttpBinding_IAuthService");
            try
            {
                var res = await Task.Run(() => client.Login(dto));
                if (res.Success)
                {
                    MessageBox.Show("Inicio de sesión exitoso, bienvenid@", "Login");
                    NavigationService?.Navigate(new MainPage());
                }
                else MessageBox.Show(res.Message, "Login");
                client.Close();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                MessageBox.Show("Servidor no encontrado en http://localhost:8085/Auth.\n¿Está corriendo el host?");
                client.Abort();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Login");
                client.Abort();
            }
        }
    }
}
