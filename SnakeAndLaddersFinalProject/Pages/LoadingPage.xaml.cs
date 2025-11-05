using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IOPath = System.IO.Path;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class LoadingPage : Page
    {
        private MediaPlayer _mediaPlayer;
        private VideoDrawing _videoDrawing;
        private DrawingBrush _drawingBrush;

        public LoadingPage()
        {
            InitializeComponent();   
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                _mediaPlayer = new MediaPlayer { Volume = 0.0, IsMuted = true };

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var videoPath = IOPath.Combine(baseDir, "Assets", "Videos", "loadingPageVideo.mp4");
                var videoUri = new Uri(videoPath, UriKind.Absolute);

                _mediaPlayer.Open(videoUri);
                _mediaPlayer.MediaOpened += (_, __) => _mediaPlayer.Play();
                _mediaPlayer.MediaEnded += (_, __) =>
                {
                    _mediaPlayer.Position = TimeSpan.Zero;
                    _mediaPlayer.Play();
                };

                _videoDrawing = new VideoDrawing
                {
                    Rect = new System.Windows.Rect(0, 0, ActualWidth, ActualHeight),
                    Player = _mediaPlayer
                };

                _drawingBrush = new DrawingBrush(_videoDrawing)
                {
                    Stretch = Stretch.UniformToFill,
                    AlignmentX = AlignmentX.Center,
                    AlignmentY = AlignmentY.Center
                };

                // 'Root' VIENE de x:Name="Root" en el XAML
                Root.Children.Clear();
                Root.Children.Add(new Rectangle { Fill = _drawingBrush });
                Root.Children.Add(new TextBlock
                {
                    Text = "Loading . . . ",
                    Foreground = Brushes.White,
                    FontSize = 28,
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                });
            }
            catch
            {
                Root.Children.Clear();
                Root.Children.Add(new Rectangle { Fill = Brushes.Black });
                Root.Children.Add(new TextBlock
                {
                    Text = "Loading...",
                    Foreground = Brushes.White,
                    FontSize = 28,
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                });
            }
        }

        private void OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (_videoDrawing != null)
                _videoDrawing.Rect = new System.Windows.Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
        }

        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                _mediaPlayer?.Stop();
                _mediaPlayer?.Close();
                _mediaPlayer = null;
            }
            catch { }
        }
    }
}
