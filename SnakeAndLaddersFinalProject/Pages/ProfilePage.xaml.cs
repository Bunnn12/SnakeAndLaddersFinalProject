using log4net;
using SnakeAndLaddersFinalProject.UserService; 
using System;
using System.Windows;
using System.Windows.Controls;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ProfilePage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ProfilePage));

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
            finally { try { client.Close(); } catch (Exception ex) { Logger.Error("Ocurrio un error", ex); } }
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

            var request = new UpdateProfileRequestDto
            {
                UserId = _loaded.UserId,
                FirstName = txtFirstName.Text?.Trim(),
                LastName = txtLastName.Text?.Trim(),
                ProfileDescription = txtDescription.Text?.Trim(),
            };

            var client = new UserServiceClient("NetTcpBinding_IUserService");
            try
            {
                var updated = client.UpdateProfile(request);

                if (updated == null)
                {
                    MessageBox.Show("No se pudo actualizar el perfil.");
                    return;
                }

                _loaded = new AccountDto
                {
                    UserId = updated.UserId,
                    Username = updated.Username,
                    FirstName = updated.FirstName,
                    LastName = updated.LastName,
                    ProfileDescription = updated.ProfileDescription,
                    Coins = updated.Coins
                };

                txtFirstName.Text = _loaded.FirstName;
                txtLastName.Text = _loaded.LastName;
                txtDescription.Text = _loaded.ProfileDescription;
                txtCoins.Text = _loaded.Coins.ToString();

                MessageBox.Show("Perfil actualizado.");
                SetEditMode(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error guardando perfil: {ex.Message}", "Error");
            }
            finally
            {
                try { client.Close(); }
                catch (Exception ex) { Logger.Error("Ocurrió un error al cerrar el cliente.", ex); }
            }
        }


        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Modo edición pendiente.");
        }
    }
}
