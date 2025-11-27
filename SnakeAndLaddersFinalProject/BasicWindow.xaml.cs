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
       

        private static readonly IReadOnlyDictionary<string, string> Backgrounds =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Auth"] = "/Assets/Images/Backgrounds/LoginBackground (2).png",
               
            };

        private const string DefaultBackground = "Assets/Images/BackgroundMainWindow.png";

        public BasicWindow()
        {
            InitializeComponent();
        }

        private async void BasicWindow_Closing(object sender, CancelEventArgs e)
        {
            
            if (MainFrame.Content is LobbyPage lobbyPage &&
                lobbyPage.DataContext is LobbyViewModel vm)
            {
                await vm.TryLeaveLobbySilentlyAsync();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            MainFrame.Navigate(new Pages.StartPage());
        }

        private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            var page = e.Content as Page;
            if (page == null)
            {
                SetBackground(DefaultBackground);
                return;
            }

            var key = SnakeAndLaddersFinalProject.Utilities.PageBackground.GetKey(page);
            if (!string.IsNullOrWhiteSpace(key) && Backgrounds.TryGetValue(key, out var path))
            {
                SetBackground(path);
            }
            else
            {
                SetBackground(DefaultBackground);
            }
        }


        private void SetBackground(string resourcePath)
        {
            try
            {
                
                var path = (resourcePath ?? string.Empty).TrimStart('/');
                var packUri = new Uri($"pack://application:,,,/{path}", UriKind.Absolute);

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
                
                var defPath = DefaultBackground.TrimStart('/');
                var defUri = new Uri($"pack://application:,,,/{defPath}", UriKind.Absolute);

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
