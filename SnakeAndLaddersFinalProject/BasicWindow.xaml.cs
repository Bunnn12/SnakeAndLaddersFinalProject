using SnakeAndLaddersFinalProject.Pages;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace SnakeAndLaddersFinalProject
{
    public sealed partial class BasicWindow : Window
    {
        private const string DEFAULT_BACKGROUND_PATH =
            "Assets/Images/BackgroundMainWindow.png";

        private const string AUTH_BACKGROUND_KEY = "Auth";
        private const string AUTH_BACKGROUND_PATH =
            "Assets/Images/Backgrounds/LoginBackground (2).png";

        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(BasicWindow));

        private static readonly IReadOnlyDictionary<string, string> _backgrounds =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [AUTH_BACKGROUND_KEY] = AUTH_BACKGROUND_PATH,
            };

        private bool _isClosingHandled;

        public BasicWindow()
        {
            InitializeComponent();
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

        private void MainFrameNavigated(object sender, NavigationEventArgs e)
        {
            var page = e.Content as Page;
            if (page == null)
            {
                SetBackground(DEFAULT_BACKGROUND_PATH);
                return;
            }

            var key = PageBackground.GetKey(page);

            if (!string.IsNullOrWhiteSpace(key) &&
                _backgrounds.TryGetValue(key, out var path))
            {
                SetBackground(path);
            }
            else
            {
                SetBackground(DEFAULT_BACKGROUND_PATH);
            }
        }

        private void SetBackground(string resourcePath)
        {
            var path = (resourcePath ?? DEFAULT_BACKGROUND_PATH).TrimStart('/');

            if (!TrySetBackgroundFromPath(path))
            {
                var defaultPath = DEFAULT_BACKGROUND_PATH.TrimStart('/');
                TrySetBackgroundFromPath(defaultPath);
            }
        }

        private bool TrySetBackgroundFromPath(string resourcePath)
        {
            try
            {
                var uri = new Uri(resourcePath, UriKind.Relative);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                BgBrush.ImageSource = bitmap;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(
                    $"Couldnt charge the image from '{resourcePath}'.",
                    ex);
                return false;
            }
        }
    }
}
