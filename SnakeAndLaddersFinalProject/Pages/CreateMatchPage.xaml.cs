using log4net;
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
        private const int BOARD_SIZE_8 = 8;
        private const int BOARD_SIZE_12 = 12;
        private const int BOARD_SIZE_DEFAULT = 10;
        private const int PLAYERS_COUNT_4 = 4;
        private const int PLAYERS_COUNT_3 = 3;
        private const int PLAYERS_COUNT_DEFAULT = 2;

        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(CreateMatchPage));

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
                    BoardSize = GetSelectedBoardSize(
                        rdbSize8x8.IsChecked,
                        rdbSize12x12.IsChecked),
                    Difficulty = GetSelectedDifficulty(
                        rdbDiffEasy.IsChecked,
                        rdbDiffHard.IsChecked),
                    SpecialTiles = GetSelectedSpecials(
                        chkSpecialDice.IsChecked,
                        chkSpecialMessage.IsChecked,
                        chkSpecialItem.IsChecked),
                    IsPrivate = chkPrivateRoom.IsChecked is true,
                    Players = GetSelectedPlayers(
                        rdbPlayers4.IsChecked,
                        rdbPlayers3.IsChecked)
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
                    "CreateMatchPage.CreateRoom",
                    _logger);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static BoardSizeOption GetSelectedBoardSize(
            bool? isSize8x8Checked,
            bool? isSize12x12Checked)
        {
            if (isSize8x8Checked is true)
            {
                return BoardSizeOption.EightByEight;
            }

            if (isSize12x12Checked is true)
            {
                return BoardSizeOption.TwelveByTwelve;
            }

            return BoardSizeOption.TenByTen;
        }

        private static DifficultyOption GetSelectedDifficulty(
            bool? isEasyChecked,
            bool? isHardChecked)
        {
            if (isEasyChecked is true)
            {
                return DifficultyOption.Easy;
            }

            if (isHardChecked is true)
            {
                return DifficultyOption.Hard;
            }

            return DifficultyOption.Medium;
        }

        private static SpecialTileOptions GetSelectedSpecials(
            bool? isSpecialDiceChecked,
            bool? isSpecialMessageChecked,
            bool? isSpecialItemChecked)
        {
            var value = SpecialTileOptions.None;

            if (isSpecialDiceChecked is true)
            {
                value |= SpecialTileOptions.Dice;
            }

            if (isSpecialMessageChecked is true)
            {
                value |= SpecialTileOptions.Message;
            }

            if (isSpecialItemChecked is true)
            {
                value |= SpecialTileOptions.Item;
            }

            return value;
        }

        private static int GetSelectedPlayers(
            bool? isPlayers4Checked,
            bool? isPlayers3Checked)
        {
            if (isPlayers4Checked is true)
            {
                return PLAYERS_COUNT_4;
            }

            if (isPlayers3Checked is true)
            {
                return PLAYERS_COUNT_3;
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
                    "CreateMatchPage.Back",
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
