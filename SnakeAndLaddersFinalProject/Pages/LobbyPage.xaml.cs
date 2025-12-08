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
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.Properties.Langs;


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
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.NavigateToBoardRequested -= OnNavigateToBoardRequested;
            viewModel.NavigateToBoardRequested += OnNavigateToBoardRequested;
            viewModel.CurrentUserKickedFromLobby -= OnCurrentUserKickedFromLobby;
            viewModel.CurrentUserKickedFromLobby += OnCurrentUserKickedFromLobby;

            try
            {
                if (args.Mode == LobbyEntryMode.Create)
                {
                    if (args.CreateOptions != null)
                    {
                        viewModel.ApplyCreateOptions(args.CreateOptions);
                    }

                    viewModel.CreateLobbyCommand?.Execute(null);
                }
                else if (args.Mode == LobbyEntryMode.Join)
                {
                    if (!string.IsNullOrWhiteSpace(args.JoinCode))
                    {
                        viewModel.CodeInput = args.JoinCode.Trim();
                        viewModel.JoinLobbyCommand?.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error inicializando LobbyPage.", ex);

                MessageBox.Show(
                    Lang.LobbyInitErrorText,
                    Lang.lblLobby,
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
            vm.CurrentUserKickedFromLobby -= OnCurrentUserKickedFromLobby;
        }

        private void OnCurrentUserKickedFromLobby()
        {
            try
            {
                BanPlayerHelper.HandleBanAndNavigateToLogin(
                    this,
                    Lang.LobbyBannedAndKickedText);

            }
            catch (Exception ex)
            {
                Logger.Error("Error al manejar el baneo del usuario actual.", ex);
            }
        }

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
                    Owner = currentWindow,
                    ShowsNavigationUI = false,
                    Content = boardPage,
                    Title = Lang.WindowTitleGameBoard
                };

                navigationWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error("Error al navegar hacia GameBoardPage.", ex);

                MessageBox.Show(
                    Lang.GameBoardOpenErrorText,
                    Lang.errorTitle,
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
                        Lang.UiInviteFriendLobbyContextMissing,
                        Lang.chatTittle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var lobbyId = lobbyViewModel.LobbyId;

                if (lobbyId <= 0)
                {
                    MessageBox.Show(
                        Lang.ChatNoActiveLobbyWarnText,
                        Lang.chatTittle,
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
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(LobbyPage)}.{nameof(OpenChat)}",
                    Logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

        private void OpenMatchInvitation(object sender, RoutedEventArgs e)
        {
            try
            {
                var lobbyViewModel = ViewModel;
                if (lobbyViewModel == null)
                {
                    MessageBox.Show(
                        Lang.UiInviteFriendLobbyContextMissing,
                        Lang.infoTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (lobbyViewModel.LobbyId <= 0 ||
                    string.IsNullOrWhiteSpace(lobbyViewModel.CodigoPartida))
                {
                    MessageBox.Show(
                        Lang.UiInviteFriendNoMatchCode,
                        Lang.infoTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var ownerWindow = Window.GetWindow(this);

                var inviteWindow = new MatchInvitationWindow(
                    lobbyViewModel.LobbyId,
                    lobbyViewModel.CodigoPartida)
                {
                    Owner = ownerWindow
                };

                inviteWindow.Show();
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(LobbyPage)}.{nameof(OpenMatchInvitation)}",
                    Logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

                bool canReport = playerReportPolicy.CanCurrentUserReportTarget(
                    currentUserId,
                    border?.DataContext);

                if (!canReport)
                {
                    return;
                }

                int reportedUserId = playerReportPolicy.GetMemberUserId(border?.DataContext);
                string reportedUserName = playerReportPolicy.GetMemberUserName(border?.DataContext);

                if (reportedUserId < 1)
                {
                    return;
                }

                var ownerWindow = Window.GetWindow(this);

                var reportsWindow = new ReportsWindow
                {
                    Owner = ownerWindow,
                    ReporterUserId = currentUserId,
                    ReportedUserId = reportedUserId,
                    ReportedUserName = reportedUserName
                };

                reportsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("Error al abrir ReportsWindow desde LobbyPage.", ex);

                MessageBox.Show(
                    Lang.ReportsWindowOpenErrorText,
                    Lang.reportUserTittle,
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
