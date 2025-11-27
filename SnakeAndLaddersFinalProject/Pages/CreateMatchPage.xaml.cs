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

        private static readonly ILog Logger = LogManager.GetLogger(typeof(CreateMatchPage));
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

             

                var args = new LobbyNavigationArgs
                {
                    Mode = LobbyEntryMode.Create,
                    CreateOptions = options
                };

                var lobbyPage = new LobbyPage(args);

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
                    Logger);

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

            if (chkSpecialTrap.IsChecked == true)
            {
                value |= SpecialTileOptions.Trap;
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
    }
}
