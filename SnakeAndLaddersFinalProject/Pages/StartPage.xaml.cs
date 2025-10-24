using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Resources;
using System.Windows.Threading;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class StartPage : Page
    {
        private Uri videoSourceUri;
        private readonly bool isMuted = true;

   
        private string _baseDir;
        private string _videosDir;
        private string _dayPath;
        private string _nightPath;
        private string _defaultPath;
        private string _currentVideoPath; 
        private DispatcherTimer _clockTimer;

        public StartPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            
            _baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _videosDir = Path.Combine(_baseDir, "Assets", "Videos");

            
            _dayPath = Path.Combine(_videosDir, "StartPageVideo.mp4");
            _nightPath = Path.Combine(_videosDir, "StartPageNightVideo.mp4");
            _defaultPath = Path.Combine(_videosDir, "StartPageVideo.mp4"); 

            
            VideoIntro.MediaOpened += OnMediaOpened;
            VideoIntro.MediaEnded += OnMediaEnded;
            VideoIntro.MediaFailed += OnMediaFailed; 
            VideoIntro.IsMuted = isMuted;
            VideoIntro.Opacity = 0; 

            
            PlayVidePerHour(initial: true);

            
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _clockTimer.Tick += (_, __) => PlayVidePerHour();
            _clockTimer.Start();
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            

            BlackTransition.IsHitTestVisible = false;

            try
            {
                _clockTimer?.Stop();

                VideoIntro.Stop();
                VideoIntro.Source = null;

                VideoIntro.MediaOpened -= OnMediaOpened;
                VideoIntro.MediaEnded -= OnMediaEnded;
                VideoIntro.MediaFailed -= OnMediaFailed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during cleanup: {ex.Message}");
            }
        }

        
        private void PlayVidePerHour(bool initial = false)
        {
            string elegido = ChooseRoutePerHour();

            if (string.IsNullOrEmpty(elegido) || !File.Exists(elegido))
            {
                
                MessageBox.Show("no se encontró ningún video para reproducir.",
                    "Video no disponible", MessageBoxButton.OK, MessageBoxImage.Warning);
                BtnStart.IsEnabled = true;
                return;
            }

            if (string.Equals(_currentVideoPath, elegido, StringComparison.OrdinalIgnoreCase))
            {
                
                if (VideoIntro.Source == null)
                {
                    videoSourceUri = new Uri(elegido, UriKind.Absolute);
                    VideoIntro.Source = videoSourceUri;
                    VideoIntro.Position = TimeSpan.Zero;
                    VideoIntro.Play();
                }
                return;
            }

            _currentVideoPath = elegido;
            RealizeSwapWithFade(elegido, initial);
        }

        private string ChooseRoutePerHour()
        {
            
            bool isDay = IsDaytime();

            string preferVideo = isDay ? _dayPath : _nightPath;
            if (File.Exists(preferVideo))
                return preferVideo;

            
            string alternVideo = isDay ? _nightPath : _dayPath;
            if (File.Exists(alternVideo))
                return alternVideo;

            
            return File.Exists(_defaultPath) ? _defaultPath : null;
        }

        private static bool IsDaytime()
        {
            int h = DateTime.Now.Hour;
            return h >= 6 && h < 18;
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            
            VideoIntro.BeginAnimation(UIElement.OpacityProperty, null);
            VideoIntro.Opacity = 1;        
            VideoIntro.Position = TimeSpan.Zero;
            VideoIntro.Play();
        }

        private void RealizeSwapWithFade(string filePath, bool initial)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"No se encontró el archivo de video:\n{filePath}");
                BtnStart.IsEnabled = true;
                return;
            }

            
            VideoIntro.BeginAnimation(UIElement.OpacityProperty, null);

            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(initial ? 0 : 250)
            };

            fadeOut.Completed += (_, __) =>
            {
                videoSourceUri = new Uri(Path.GetFullPath(filePath), UriKind.Absolute);
                VideoIntro.Source = videoSourceUri;
                VideoIntro.Position = TimeSpan.Zero;
                VideoIntro.Play();

                var fadeIn = new DoubleAnimation
                {
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(initial ? 0 : 250)
                };
                VideoIntro.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };

            
            if (initial)
            {
                
                videoSourceUri = new Uri(Path.GetFullPath(filePath), UriKind.Absolute);
                VideoIntro.Source = videoSourceUri;
                VideoIntro.Position = TimeSpan.Zero;
                VideoIntro.Play();

                var fadeIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(0) };
                VideoIntro.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
            else
            {
                VideoIntro.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
        }


        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            VideoIntro.Position = TimeSpan.Zero; 
            VideoIntro.Play();
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var src = VideoIntro.Source?.ToString() ?? "(sin Source)";
            MessageBox.Show(
                $"No se pudo cargar el video:\n{src}\n\n" +
                $"Existe en disco: {(VideoIntro.Source != null && File.Exists(VideoIntro.Source.LocalPath))}\n\n" +
                $"Error: {e.ErrorException?.Message}",
                "Video no cargó", MessageBoxButton.OK, MessageBoxImage.Warning);

            
            BtnStart.IsEnabled = true;
        }

        
        private void StartGame(object sender, RoutedEventArgs e)
        {
            PlayClickSound();

            var fadeOut = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(1),
                FillBehavior = FillBehavior.HoldEnd
            };

            fadeOut.Completed += (s, _) =>
            {
                NavigationService?.Navigate(new LoginPage());
            };

            BlackTransition.IsHitTestVisible = true;
            BtnStart.IsEnabled = false;
            BlackTransition.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private static void PlayClickSound()
        {
            try
            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets", "Sounds", "confirmation_002.wav");

                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"No se encontró el archivo de sonido:\n{filePath}");
                    return;
                }

                using (var player = new SoundPlayer(filePath))
                {
                    player.Play(); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al reproducir sonido: {ex.Message}");
            }
        }
    }
}
