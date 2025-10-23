using log4net;
using SnakeAndLaddersFinalProject.Navigation;
using SnakeAndLaddersFinalProject.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LobbyPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LobbyPage));

        private readonly LobbyNavigationArgs _args;

        
        public LobbyPage() : this(new LobbyNavigationArgs { Mode = LobbyEntryMode.Create }) { }

        public LobbyPage(LobbyNavigationArgs args)
        {
            InitializeComponent();
            _args = args ?? new LobbyNavigationArgs { Mode = LobbyEntryMode.Create };
            this.DataContext = new LobbyViewModel();

            Loaded += LobbyPageLoaded;
        }

        private void LobbyPageLoaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as LobbyViewModel;
            if (vm == null) return;

            
            if (_args.Mode == LobbyEntryMode.Create)
            {
                
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

        private void OpenChat(object sender, RoutedEventArgs e)
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
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("No se pudo navegar a la página de chat.",
                                "Error de navegación", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error("Error al abrir la página de chat.", ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrió un error inesperado al intentar abrir el chat.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error("Error inesperado al abrir la página de chat.", ex);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
