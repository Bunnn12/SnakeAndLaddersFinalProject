using log4net;
using SnakeAndLaddersFinalProject.Navigation;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.Windows;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LobbyPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LobbyPage));

        private readonly LobbyNavigationArgs args;

        private LobbyViewModel ViewModel
        {
            get { return DataContext as LobbyViewModel; }
        }

        public LobbyPage()
            : this(new LobbyNavigationArgs { Mode = LobbyEntryMode.Create })
        {
        }

        public LobbyPage(LobbyNavigationArgs value)
        {
            InitializeComponent();

            args = value ?? new LobbyNavigationArgs { Mode = LobbyEntryMode.Create };
            DataContext = new LobbyViewModel();

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

            vm.NavigateToBoardRequested -= OnNavigateToBoardRequested;
            vm.NavigateToBoardRequested += OnNavigateToBoardRequested;

            try
            {
                if (args.Mode == LobbyEntryMode.Create)
                {
                    if (args.CreateOptions != null)
                    {
                        vm.ApplyCreateOptions(args.CreateOptions);
                    }

                    vm.CreateLobbyCommand?.Execute(null);
                }
                else if (args.Mode == LobbyEntryMode.Join)
                {
                    if (!string.IsNullOrWhiteSpace(args.JoinCode))
                    {
                        vm.CodigoInput = args.JoinCode.Trim();
                        vm.JoinLobbyCommand?.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error inicializando LobbyPage.", ex);

                MessageBox.Show(
                    "No fue posible inicializar el lobby.",
                    "Lobby",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel;
            if (vm == null)
            {
                return;
            }

            vm.NavigateToBoardRequested -= OnNavigateToBoardRequested;
        }

        private void OnNavigateToBoardRequested(object sender, SnakeAndLaddersFinalProject.CreateMatchOptions options)
        {
            if (options == null)
            {
                options = new SnakeAndLaddersFinalProject.CreateMatchOptions();
            }

            var boardPage = new GameBoardPage(options);

            try
            {
                if (NavigationService != null)
                {
                    NavigationService.Navigate(boardPage);
                    return;
                }

                var currentWindow = Window.GetWindow(this);
                var mainFrame = currentWindow?.FindName("MainFrame") as Frame;
                if (mainFrame != null)
                {
                    mainFrame.Navigate(boardPage);
                    return;
                }

                var navigationWindow = new NavigationWindow
                {
                    ShowsNavigationUI = true
                };

                navigationWindow.Navigate(boardPage);
                navigationWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error("Error al navegar hacia GameBoardPage.", ex);

                MessageBox.Show(
                    "No fue posible abrir el tablero de juego.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenChat(object sender, RoutedEventArgs e)
        {
            try
            {
                var lobbyViewModel = ViewModel;

                if (lobbyViewModel == null)
                {
                    MessageBox.Show(
                        "No se encontró el contexto del lobby.",
                        "Chat",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var lobbyId = lobbyViewModel.LobbyId;

                if (lobbyId <= 0)
                {
                    MessageBox.Show(
                        "Aún no hay un lobby activo. Crea o únete antes de abrir el chat.",
                        "Chat",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                var ownerWindow = Window.GetWindow(this);

                var chatWindow = new ChatWindow(lobbyId)
                {
                    Owner = ownerWindow
                };

                chatWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ocurrió un error inesperado al intentar abrir el chat.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Logger.Error("Error inesperado al abrir la ventana de chat.", ex);
            }
        }

        private void LeaveLobby(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel;
            vm?.LeaveLobbyCommand?.Execute(null);
        }

        private void StartMatch(object sender, RoutedEventArgs e)
        {
            var vm = ViewModel;
            vm?.StartMatchCommand?.Execute(null);
        }
    }
}
