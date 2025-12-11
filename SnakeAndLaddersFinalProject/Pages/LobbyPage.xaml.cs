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
        private const int MIN_VALID_USER_ID = 1;
        private const string CONTEXT_MENU_TAG_REPORT = "Report";
        private const string CONTEXT_MENU_TAG_KICK = "Kick";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyPage));

        private readonly LobbyNavigationArgs _lobbyNavigationArgs;
        private readonly PlayerReportPolicy _playerReportPolicy = new PlayerReportPolicy();

        private LobbyViewModel ViewModel
        {
            get { return DataContext as LobbyViewModel; }
        }

        public LobbyPage() : this(new LobbyNavigationArgs { Mode = LobbyEntryMode.Create })
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
                _logger.Error("Error initializing LobbyPage.", ex);

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

                BanPlayerHelper.HandleBanAndNavigateToLogin(this, message);
            }
            catch (Exception ex)
            {
                _logger.Error("Error handling current user kicked event.", ex);
            }
        }

        private void OnNavigateToBoardRequested(GameBoardViewModel boardViewModel)
        {
            if (boardViewModel == null)
            {
                _logger.Warn("NavigateToBoardRequested received null _viewModel.");
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
                _logger.Error("Error navigating to GameBoardPage.", ex);

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
                _logger.Error("Error navigating to MainPage.", ex);
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
                string userMessage = ExceptionHandler.Handle(ex, "LobbyPage.OpenChat",
                    _logger);
                MessageBox.Show(userMessage, Lang.errorTitle, MessageBoxButton.OK,
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

                if (lobbyViewModel.LobbyId <= 0 || string.IsNullOrWhiteSpace(lobbyViewModel.
                    CodigoPartida))
                {
                    MessageBox.Show(
                        Lang.UiInviteFriendNoMatchCode,
                        Lang.infoTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var ownerWindow = Window.GetWindow(this);
                var inviteWindow = new MatchInvitationWindow(lobbyViewModel.LobbyId,
                    lobbyViewModel.CodigoPartida)
                {
                    Owner = ownerWindow
                };
                inviteWindow.Show();
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(ex, "LobbyPage.OpenMatchInvitation",
                    _logger);
                MessageBox.Show(userMessage, Lang.errorTitle, MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ReportPlayerMenuItem(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(sender is MenuItem menuItem))
                {
                    return;
                }

                if (!(menuItem.DataContext is LobbyMemberViewModel member))
                {
                    return;
                }

                int currentUserId = SessionContext.Current.UserId;

                if (!_playerReportPolicy.CanCurrentUserReportTarget(currentUserId, member))
                {
                    return;
                }

                int reportedUserId = _playerReportPolicy.GetMemberUserId(member);
                string reportedUserName = _playerReportPolicy.GetMemberUserName(member);

                if (reportedUserId < MIN_VALID_USER_ID)
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
                _logger.Error("Error opening ReportsWindow.", ex);
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
                if (!(sender is Button button))
                {
                    return;
                }

                var member = button.Tag as LobbyMemberViewModel;
                var viewModel = ViewModel;

                if (member == null || viewModel == null || button.ContextMenu == null)
                {
                    return;
                }

                int currentUserId = SessionContext.Current.UserId;
                bool canReport = _playerReportPolicy.CanCurrentUserReportTarget(currentUserId, member);
                bool canKick = viewModel.CanCurrentUserKickMember(member);

                foreach (var item in button.ContextMenu.Items.OfType<MenuItem>())
                {
                    if (Equals(item.Tag, CONTEXT_MENU_TAG_REPORT))
                    {
                        item.Visibility = canReport ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else if (Equals(item.Tag, CONTEXT_MENU_TAG_KICK))
                    {
                        item.Visibility = canKick ? Visibility.Visible : Visibility.Collapsed;
                    }
                }

                if (!canReport && !canKick)
                {
                    return;
                }

                button.ContextMenu.DataContext = member;
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error opening lobby member context menu.", ex);
            }
        }

        private async void KickPlayerMenuItem(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(sender is MenuItem menuItem))
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
                _logger.Error("Error kicking player from menu item.", ex);
                MessageBox.Show(
                    Lang.LobbyKickFailedText,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
