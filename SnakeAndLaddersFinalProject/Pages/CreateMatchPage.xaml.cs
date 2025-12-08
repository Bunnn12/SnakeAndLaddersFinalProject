using log4net;
using log4net.Repository.Hierarchy;
using SnakeAndLaddersFinalProject.Navigation;
using SnakeAndLaddersFinalProject.Properties.Langs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Utilities;


namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class CreateMatchPage : Page
    {

        private static readonly ILog _logger = LogManager.GetLogger(typeof(CreateMatchPage));
        public CreateMatchPage()
        {
            InitializeComponent();
        }
        private void CreateRoom(object sender, RoutedEventArgs e)
        {
            try
            {
                var options = new CreateMatchOptions
                {
                    BoardSize = GetSelectedBoardSize(),
                    Difficulty = GetSelectedDifficulty(),
                    SpecialTiles = GetSelectedSpecials(),
                    IsPrivate = chkPrivateRoom.IsChecked == true,
                    Players = GetSelectedPlayers()
                };

             

                var lobbyNavigationArgs = new LobbyNavigationArgs
                {
                    Mode = LobbyEntryMode.Create,
                    CreateOptions = options
                };

                var lobbyPage = new LobbyPage(lobbyNavigationArgs);

                if (NavigationService != null)
                {
                    NavigationService.Navigate(lobbyPage);
                }
                else
                {
                    var window = Application.Current.MainWindow as NavigationWindow;
                    if (window != null)
                    {
                        window.Navigate(lobbyPage);
                    }
                    else
                    {
                        Application.Current.MainWindow.Content = lobbyPage;
                    }
                }
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(CreateMatchPage)}.{nameof(CreateRoom)}",
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private BoardSizeOption GetSelectedBoardSize()
        {
            if (rdbSize8x8.IsChecked == true)
            {
                return BoardSizeOption.EightByEight;
            }

            if (rdbSize12x12.IsChecked == true)
            {
                return BoardSizeOption.TwelveByTwelve;
            }

            return BoardSizeOption.TenByTen;
        }

        private DifficultyOption GetSelectedDifficulty()
        {
            if (rdbDiffEasy.IsChecked == true)
            {
                return DifficultyOption.Easy;
            }

            if (rdbDiffHard.IsChecked == true)
            {
                return DifficultyOption.Hard;
            }

            return DifficultyOption.Medium;
        }

        private SpecialTileOptions GetSelectedSpecials()
        {
            var value = SpecialTileOptions.None;

            if (chkSpecialDice.IsChecked == true)
            {
                value |= SpecialTileOptions.Dice;
            }

            if (chkSpecialMessage.IsChecked == true)
            {
                value |= SpecialTileOptions.Message;
            }

            if (chkSpecialItem.IsChecked == true)
            {
                value |= SpecialTileOptions.Item;
            }

            return value;
        }

        private int GetSelectedPlayers()
        {
            if (rdbPlayers4.IsChecked == true)
            {
                return 4;
            }

            if (rdbPlayers3.IsChecked == true)
            {
                return 3;
            }

            return AppConstants.DEFAULT_PLAYERS;
        }
        private void Back(object sender, RoutedEventArgs e)
        {
            try
            {
                var navigationService = NavigationService.GetNavigationService(this);
                if (navigationService != null && navigationService.CanGoBack)
                {
                    navigationService.GoBack();
                    return;
                }

                var window = Application.Current.MainWindow as NavigationWindow;
                if (window != null && window.CanGoBack)
                {
                    window.GoBack();
                    return;
                }

                MessageBox.Show(
                    Lang.UiNavigationNoHistory,
                    Lang.UiTitleInfo,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(CreateMatchPage)}.{nameof(Back)}",
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
