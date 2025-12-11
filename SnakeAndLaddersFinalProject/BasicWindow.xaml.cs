using SnakeAndLaddersFinalProject.Pages;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;
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
        private const string DEFAULT_BACKGROUND_PATH = "Assets/Images/BackgroundMainWindow.png";
        private const string AUTH_BACKGROUND_KEY = "Auth";
        private const string AUTH_BACKGROUND_PATH = "/Assets/Images/Backgrounds/LoginBackground (2).png";
        private const string PACK_URI_FORMAT = "pack://application:,,,/{0}";

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

        private async void BasicWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_isClosingHandled)
            {
                return;
            }

            _isClosingHandled = true;

            if (MainFrame.Content is LobbyPage lobbyPage &&
                lobbyPage.DataContext is LobbyViewModel vm)
            {
                await vm.TryLeaveLobbySilentlyAsync().ConfigureAwait(true);
            }

            try
            {
                await AuthClientHelper.LogoutAsync().ConfigureAwait(true);
            }
            catch
            {
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.StartPage());
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var page = e.Content as Page;
            if (page == null)
            {
                SetBackground(DEFAULT_BACKGROUND_PATH);
                return;
            }

            var key = PageBackground.GetKey(page);

            if (!string.IsNullOrWhiteSpace(key) && _backgrounds.TryGetValue(key, out var path))
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
            try
            {
                var path = (resourcePath ?? string.Empty).TrimStart('/');
                var packUri = new Uri(string.Format(PACK_URI_FORMAT, path), UriKind.Absolute);

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = packUri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                BgBrush.ImageSource = bmp;
            }
            catch
            {
                var defPath = DEFAULT_BACKGROUND_PATH.TrimStart('/');
                var defUri = new Uri(string.Format(PACK_URI_FORMAT, defPath), UriKind.Absolute);

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = defUri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                BgBrush.ImageSource = bmp;
            }
        }
    }
}
