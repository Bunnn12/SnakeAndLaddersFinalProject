using SnakeAndLaddersFinalProject.Pages;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;
using log4net;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging; 

namespace SnakeAndLaddersFinalProject
{
    public sealed partial class BasicWindow : Window
    {
        private const string DEFAULT_BACKGROUND_PATH = "Assets/Images/BackgroundMainWindow.png";

        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(BasicWindow));

        private bool _isClosingHandled;

        public BasicWindow()
        {
            InitializeComponent();
            LoadDefaultBackground();
        }

        private async void BasicWindowClosing(object sender, CancelEventArgs e)
        {
            if (_isClosingHandled)
            {
                return;
            }

            _isClosingHandled = true;

            if (MainFrame.Content is LobbyPage lobbyPage &&
                lobbyPage.DataContext is LobbyViewModel lobbyViewModel)
            {
                await lobbyViewModel
                    .TryLeaveLobbySilentlyAsync()
                    .ConfigureAwait(true);
            }

            try
            {
                await AuthClientHelper
                    .LogoutAsync()
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _logger.Warn("Logout error, an error ocurred while closing the window", ex);
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new StartPage());
        }

        private void LoadDefaultBackground()
        {
            try
            {
                var uri = new Uri(DEFAULT_BACKGROUND_PATH, UriKind.Relative);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                BgBrush.ImageSource = bitmap;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not load default background from '{DEFAULT_BACKGROUND_PATH}'.", ex);
            }
        }
    }
}
