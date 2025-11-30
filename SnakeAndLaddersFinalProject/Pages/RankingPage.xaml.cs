using System.Windows;
using System.Windows.Controls;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class RankingPage : Page
    {
        private RankingViewModel ViewModel
        {
            get { return DataContext as RankingViewModel; }
        }

        public RankingPage()
        {
            InitializeComponent();

            DataContext = new RankingViewModel();

            Loaded -= OnLoaded;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.LoadRanking();
        }

        private void Back(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}
