using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Navigation;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LobbyPage : Page
    {
        private readonly LobbyNavigationArgs _args;

        // ctor por defecto, si lo necesitas en diseñador
        public LobbyPage() : this(new LobbyNavigationArgs { Mode = LobbyEntryMode.Create }) { }

        public LobbyPage(LobbyNavigationArgs args)
        {
            InitializeComponent();
            _args = args ?? new LobbyNavigationArgs { Mode = LobbyEntryMode.Create };
            this.DataContext = new LobbyViewModel();

            Loaded += LobbyPage_Loaded;
        }

        private void LobbyPage_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as LobbyViewModel;
            if (vm == null) return;

            // Ejecuta automáticamente según el modo
            if (_args.Mode == LobbyEntryMode.Create)
            {
                // Crea el lobby y entra como host
                vm.CreateLobbyCommand.Execute(null);
            }
            else if (_args.Mode == LobbyEntryMode.Join)
            {
                if (!string.IsNullOrWhiteSpace(_args.JoinCode))
                {
                    vm.CodigoInput = _args.JoinCode.Trim();
                    vm.JoinLobbyCommand.Execute(null);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var vm = DataContext as LobbyViewModel;
                int lobbyId = vm?.LobbyId ?? 0;

                if (lobbyId <= 0)
                {
                    MessageBox.Show("Aún no hay un lobby activo. Crea o únete antes de abrir el chat.",
                                    "Chat", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var chatPage = new ChatPage(lobbyId);

                if (NavigationService != null)
                {
                    NavigationService.Navigate(chatPage);
                    return;
                }

                Window currentWindow = Window.GetWindow(this);
                var mainFrame = currentWindow?.FindName("MainFrame") as Frame;
                if (mainFrame != null)
                {
                    mainFrame.Navigate(chatPage);
                    return;
                }

                var navWindow = new NavigationWindow { ShowsNavigationUI = true };
                navWindow.Navigate(chatPage);
                navWindow.Show();
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("hay una ex, cambiar luego a una ventana de dialogo :)");
            }
            catch (Exception)
            {
                Console.WriteLine("hay una ex, cambiar luego");
            }
        }


    }
}
