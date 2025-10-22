using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject
{
    public sealed partial class BasicWindow : Window
    {
        // Mapas de clave lógica → ruta de imagen (reutiliza "Auth" para Login y SignUp)
        private static readonly IReadOnlyDictionary<string, string> Backgrounds =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Auth"] = "/Assets/Images/Backgrounds/LoginBackground (2).png",
                //["Lobby"] = "/Assets/Images/BackgroundLobby.png" para cuando se agregue un fondo distinto al lobby o alguna otra 
            };

        private const string DefaultBackground = "Assets/Images/BackgroundMainWindow.png";

        public BasicWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Página inicial: StartPage
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
                // Normaliza y arma pack URI: pack://application:,,,/Assets/Images/xxx.png
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
                // Fallback al default
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
