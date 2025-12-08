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
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class StartPage : Page
    {
        private const int DAYTIME_START_HOUR = 6;
        private const int DAYTIME_END_HOUR = 18;

        private Uri _videoSourceUri;
        private readonly bool _isMuted = true;

        private string _baseDir;
        private string _videosDir;
        private string _dayVideoPath;
        private string _nightVideoPath;
        private string _defaultVideoPath;
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

            
            _dayVideoPath = Path.Combine(_videosDir, "StartPageVideo.mp4");
            _nightVideoPath = Path.Combine(_videosDir, "StartPageNightVideo.mp4");
            _defaultVideoPath = Path.Combine(_videosDir, "StartPageVideo.mp4"); 

            
            VideoIntro.MediaOpened += OnMediaOpened;
            VideoIntro.MediaEnded += OnMediaEnded;
            VideoIntro.MediaFailed += OnMediaFailed; 
            VideoIntro.IsMuted = _isMuted;
            VideoIntro.Opacity = 0; 

            
            UpdateVideoForCurrentTime(initial: true);

            
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _clockTimer.Tick += (_, __) => UpdateVideoForCurrentTime();
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
                MessageBox.Show(Lang.StartCleanupErrorText, Lang.UiTitleError,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        
        private void UpdateVideoForCurrentTime(bool initial = false)
        {
            string selectedVideoPath = GetVideoPathForCurrentTime();

            if (string.IsNullOrEmpty(selectedVideoPath) || !File.Exists(selectedVideoPath))
            {

                MessageBox.Show(Lang.StartVideoNotFoundText, Lang.StartVideoNotAvailableTitle,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BtnStart.IsEnabled = true;
                return;
            }

            if (string.Equals(_currentVideoPath, selectedVideoPath, StringComparison.OrdinalIgnoreCase))
            {
                
                if (VideoIntro.Source == null)
                {
                    _videoSourceUri = new Uri(selectedVideoPath, UriKind.Absolute);
                    VideoIntro.Source = _videoSourceUri;
                    VideoIntro.Position = TimeSpan.Zero;
                    VideoIntro.Play();
                }
                return;
            }

            _currentVideoPath = selectedVideoPath;
            SwapWithFade(selectedVideoPath, initial);
        }

        private string GetVideoPathForCurrentTime()
        {
            
            bool isDay = IsDaytime();

            string preferVideo = isDay ? _dayVideoPath : _nightVideoPath;
            if (File.Exists(preferVideo))
                return preferVideo;

            
            string alternVideo = isDay ? _nightVideoPath : _dayVideoPath;
            if (File.Exists(alternVideo))
                return alternVideo;

            
            return File.Exists(_defaultVideoPath) ? _defaultVideoPath : null;
        }

        private static bool IsDaytime()
        {
            int currentHour = DateTime.Now.Hour;
            return currentHour >= DAYTIME_START_HOUR && currentHour < DAYTIME_END_HOUR;
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            
            VideoIntro.BeginAnimation(UIElement.OpacityProperty, null);
            VideoIntro.Opacity = 1;        
            VideoIntro.Position = TimeSpan.Zero;
            VideoIntro.Play();
        }

        private void SwapWithFade(string filePath, bool initial)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    string.Format(Lang.StartVideoFileNotFoundFmt, Environment.NewLine, filePath),
                    Lang.UiTitleError, MessageBoxButton.OK, MessageBoxImage.Error);
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
                _videoSourceUri = new Uri(Path.GetFullPath(filePath), UriKind.Absolute);
                VideoIntro.Source = _videoSourceUri;
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
                
                _videoSourceUri = new Uri(Path.GetFullPath(filePath), UriKind.Absolute);
                VideoIntro.Source = _videoSourceUri;
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
            MessageBox.Show(Lang.StartVideoLoadFailedText, Lang.StartVideoLoadFailedTitle,
                MessageBoxButton.OK, MessageBoxImage.Warning);

            BtnStart.IsEnabled = true;
        }

        
        private void Start(object sender, RoutedEventArgs e)
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
                    MessageBox.Show(
                        string.Format(Lang.StartSoundFileNotFoundFmt, Environment.NewLine, filePath),
                        Lang.UiTitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var player = new SoundPlayer(filePath))
                {
                    player.Play(); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.StartSoundPlayErrorText, Lang.UiTitleError,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
