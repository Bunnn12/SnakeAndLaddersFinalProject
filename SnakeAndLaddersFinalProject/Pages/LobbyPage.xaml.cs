using System;
using System.Linq;
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
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LobbyPage : Page
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyPage));

        private readonly LobbyNavigationArgs _lobbyNavigationArgs;

        private readonly PlayerReportPolicy _playerReportPolicy = new PlayerReportPolicy();

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

            _lobbyNavigationArgs = value ?? new LobbyNavigationArgs { Mode = LobbyEntryMode.Create };
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

            viewModel.NavigateToMainPageRequested -= OnNavigateToMainPageRequested;
            viewModel.NavigateToMainPageRequested += OnNavigateToMainPageRequested;

            try
            {
                if (_lobbyNavigationArgs.Mode == LobbyEntryMode.Create)
                {
                    if (_lobbyNavigationArgs.CreateOptions != null)
                    {
                        viewModel.ApplyCreateOptions(_lobbyNavigationArgs.CreateOptions);
                    }

                    viewModel.CreateLobbyCommand?.Execute(null);
                }
                else if (_lobbyNavigationArgs.Mode == LobbyEntryMode.Join)
                {
                    if (!string.IsNullOrWhiteSpace(_lobbyNavigationArgs.JoinCode))
                    {
                        viewModel.CodeInput = _lobbyNavigationArgs.JoinCode.Trim();
                        viewModel.JoinLobbyCommand?.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error inicializando LobbyPage.", ex);

                MessageBox.Show(
                    Lang.LobbyInitErrorText,
                    Lang.lblLobby,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.NavigateToBoardRequested -= OnNavigateToBoardRequested;
            viewModel.CurrentUserKickedFromLobby -= OnCurrentUserKickedFromLobby;
            viewModel.NavigateToMainPageRequested -= OnNavigateToMainPageRequested;
        }

        private void OnCurrentUserKickedFromLobby()
        {
            try
            {
                var viewModel = ViewModel;
                if (viewModel == null)
                {
                    return;
                }

                if (viewModel.WasLastKickByHost)
                {
                    MessageBox.Show(
                        Lang.LobbyKickedByHostText,
                        Lang.warningTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    OnNavigateToMainPageRequested();
                    return;
                }

                string message = !string.IsNullOrWhiteSpace(viewModel.KickMessageForLogin)
                    ? viewModel.KickMessageForLogin
                    : Lang.LobbyBannedAndKickedText;

                BanPlayerHelper.HandleBanAndNavigateToLogin(
                    this,
                    message);
            }
            catch (Exception ex)
            {
                _logger.Error("Error al manejar el baneo o expulsión del usuario actual.", ex);
            }
        }

        private void OnNavigateToBoardRequested(GameBoardViewModel boardViewModel)
        {
            if (boardViewModel == null)
            {
                _logger.Warn("Se recibió una solicitud de navegación al tablero sin boardViewModel.");
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
                _logger.Error("Error al navegar hacia GameBoardPage.", ex);

                MessageBox.Show(
                    Lang.GameBoardOpenErrorText,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnNavigateToMainPageRequested()
        {
            try
            {
                if (NavigationService != null)
                {
                    NavigationService.Navigate(new MainPage());
                    return;
                }

                var currentWindow = Window.GetWindow(this);
                var mainFrame = currentWindow?.FindName("MainFrame") as Frame;
                if (mainFrame != null)
                {
                    mainFrame.Navigate(new MainPage());
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error al navegar de LobbyPage hacia MainPage.", ex);
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
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LeaveLobby(object sender, RoutedEventArgs e)
        {
            var lobbyViewModel = ViewModel;
            lobbyViewModel?.LeaveLobbyCommand?.Execute(null);
        }

        private void StartMatch(object sender, RoutedEventArgs e)
        {
            var lobbyViewModel = ViewModel;
            lobbyViewModel?.StartMatchCommand?.Execute(null);
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
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ReportPlayerMenuItem(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;
                if (menuItem == null)
                {
                    return;
                }

                var member = menuItem.DataContext as LobbyMemberViewModel;
                if (member == null)
                {
                    return;
                }

                int currentUserId = SessionContext.Current.UserId;

                bool canReport = _playerReportPolicy.CanCurrentUserReportTarget(
                    currentUserId,
                    member);

                if (!canReport)
                {
                    return;
                }

                int reportedUserId = _playerReportPolicy.GetMemberUserId(member);
                string reportedUserName = _playerReportPolicy.GetMemberUserName(member);

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
                _logger.Error("Error al abrir ReportsWindow desde LobbyPage.", ex);

                MessageBox.Show(
                    Lang.ReportsWindowOpenErrorText,
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnLobbyMemberActionsButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button == null)
                {
                    return;
                }

                var member = button.Tag as LobbyMemberViewModel;
                var viewModel = ViewModel;

                if (member == null || viewModel == null)
                {
                    return;
                }

                var contextMenu = button.ContextMenu;
                if (contextMenu == null)
                {
                    return;
                }

                int currentUserId = SessionContext.Current.UserId;

                bool canReport = _playerReportPolicy.CanCurrentUserReportTarget(
                    currentUserId,
                    member);

                bool canKick = viewModel.CanCurrentUserKickMember(member);

                foreach (var item in contextMenu.Items.OfType<MenuItem>())
                {
                    if (Equals(item.Tag, "Report"))
                    {
                        item.Visibility = canReport
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    }
                    else if (Equals(item.Tag, "Kick"))
                    {
                        item.Visibility = canKick
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    }
                }

                if (!canReport && !canKick)
                {
                    return;
                }

                contextMenu.DataContext = member;
                contextMenu.PlacementTarget = button;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error al abrir el menú de acciones del miembro del lobby.", ex);
            }
        }

        private async void KickPlayerMenuItem(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;
                if (menuItem == null)
                {
                    return;
                }

                var member = menuItem.DataContext as LobbyMemberViewModel;
                var viewModel = ViewModel;

                if (member == null || viewModel == null)
                {
                    return;
                }

                if (!viewModel.CanCurrentUserKickMember(member))
                {
                    return;
                }

                var result = MessageBox.Show(
                    string.Format(Lang.LobbyKickConfirmTextFmt, member.UserName),
                    Lang.LobbyKickConfirmTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);


                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                await viewModel.KickMemberAsync(member);
            }
            catch (Exception ex)
            {
                _logger.Error("Error al expulsar a un miembro del lobby desde el menú contextual.", ex);

                MessageBox.Show(
                    Lang.LobbyKickFailedText,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

            }
        }
    }
}
