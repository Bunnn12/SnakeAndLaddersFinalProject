using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Navigation;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.Windows;
using SnakeAndLaddersFinalProject.Policies;


namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LobbyPage : Page
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LobbyPage));

        private readonly LobbyNavigationArgs args;

        private readonly PlayerReportPolicy playerReportPolicy = new PlayerReportPolicy();

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

            // Suscribimos el evento de navegación al tablero
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

        // AHORA el handler recibe directamente el GameBoardViewModel
        private void OnNavigateToBoardRequested(GameBoardViewModel boardViewModel)
        {
            if (boardViewModel == null)
            {
                Logger.Warn("Se recibió una solicitud de navegación al tablero sin ViewModel.");
                return;
            }

            var boardPage = new GameBoardPage
            {
                DataContext = boardViewModel
            };

            try
            {
                // 1) Si esta Page tiene NavigationService, úsalo
                if (NavigationService != null)
                {
                    NavigationService.Navigate(boardPage);
                    return;
                }

                // 2) Si la ventana principal tiene un Frame llamado MainFrame, úsalo
                var currentWindow = Window.GetWindow(this);
                var mainFrame = currentWindow?.FindName("MainFrame") as Frame;
                if (mainFrame != null)
                {
                    mainFrame.Navigate(boardPage);
                    return;
                }

                // 3) Último recurso: NavigationWindow independiente
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

        private void MemberBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var border = sender as Border;
                if (border == null)
                {
                    return;
                }

                int currentUserId = SessionContext.Current.UserId;

                bool canReport = playerReportPolicy.CanCurrentUserReportTarget(currentUserId, border.DataContext);
                if (!canReport)
                {
                    return;
                }

                var contextMenu = border.ContextMenu;
                if (contextMenu == null)
                {
                    return;
                }

                contextMenu.DataContext = border.DataContext;
                contextMenu.PlacementTarget = border;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error al mostrar el menú contextual del miembro del lobby.", ex);
            }
        }


        private void ReportPlayerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;
                if (menuItem == null)
                {
                    return;
                }

                var contextMenu = menuItem.Parent as ContextMenu;
                var border = contextMenu?.PlacementTarget as Border;

                int currentUserId = SessionContext.Current.UserId;

                bool canReport = playerReportPolicy.CanCurrentUserReportTarget(currentUserId, border?.DataContext);
                if (!canReport)
                {
                    return;
                }

                var ownerWindow = Window.GetWindow(this);

                var reportsWindow = new ReportsWindow
                {
                    Owner = ownerWindow
                };

                reportsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("Error al abrir ReportsWindow desde LobbyPage.", ex);

                MessageBox.Show(
                    "Ocurrió un error al intentar abrir la ventana de reportes.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PlayerBorder_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                var border = sender as Border;
                if (border == null)
                {
                    return;
                }

                int currentUserId = SessionContext.Current.UserId;

                bool canReport = playerReportPolicy.CanCurrentUserReportTarget(currentUserId, border.DataContext);
                if (!canReport)
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error al validar la apertura del menú contextual del miembro del lobby.", ex);
            }
        }

    }
}
