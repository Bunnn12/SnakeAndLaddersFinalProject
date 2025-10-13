using System;
using System.Windows;
using System.Windows.Controls;
using SnakeAndLaddersFinalProject.UserService; 

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ProfilePage : Page
    {
        private readonly string _username;

        private AccountDto _loaded;


        public ProfilePage(string username)
        {
            InitializeComponent();
            _username = username ?? throw new ArgumentNullException(nameof(username));
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CargarPerfil();
            SetEditMode(false);
        }

        private void CargarPerfil()
        {
            var client = new UserServiceClient("NetTcpBinding_IUserService"); // o HTTP
            try
            {
                _loaded = client.GetProfileByUsername(_username);
                if (_loaded == null) { MessageBox.Show("Perfil no encontrado."); return; }

                txtUsername.Text = _loaded.Username;
                txtFirstName.Text = _loaded.FirstName;
                txtLastName.Text = _loaded.LastName;
                txtDescription.Text = _loaded.ProfileDescription;
                txtCoins.Text = _loaded.Coins.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando perfil: {ex.Message}", "Error");
            }
            finally { try { client.Close(); } catch { } }
        }

        private void SetEditMode(bool enabled)
        {
            txtFirstName.IsReadOnly = !enabled;
            txtLastName.IsReadOnly = !enabled;
            txtDescription.IsReadOnly = !enabled;
            txtCoins.IsReadOnly = !enabled;

            btnEditar.IsEnabled = !enabled;
            btnGuardar.IsEnabled = enabled;
            btnCancelar.IsEnabled = enabled;
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e) => SetEditMode(true);

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            // restaurar valores originales
            if (_loaded != null)
            {
                txtFirstName.Text = _loaded.FirstName;
                txtLastName.Text = _loaded.LastName;
                txtDescription.Text = _loaded.ProfileDescription;
                txtCoins.Text = _loaded.Coins.ToString();
            }
            SetEditMode(false);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_loaded == null) return;

            if (!int.TryParse(txtCoins.Text, out int coins))
            {
                MessageBox.Show("Monedas inválidas.");
                return;
            }

            var dto = new AccountDto
            {
                UserId = _loaded.UserId,
                Username = _loaded.Username, // no lo cambiamos aquí
                FirstName = txtFirstName.Text?.Trim(),
                LastName = txtLastName.Text?.Trim(),
                ProfileDescription = txtDescription.Text?.Trim(),
                Coins = coins
            };

            var client = new UserServiceClient("NetTcpBinding_IUserService");
            try
            {
                var ok = client.UpdateProfile(dto);
                if (ok)
                {
                    MessageBox.Show("Perfil actualizado.");
                    _loaded = dto; // refresca cache local
                    SetEditMode(false);
                }
                else
                {
                    MessageBox.Show("No se actualizó el perfil (verifica el usuario).");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error guardando perfil: {ex.Message}", "Error");
            }
            finally { try { client.Close(); } catch { } }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Modo edición pendiente.");
        }
    }
}
