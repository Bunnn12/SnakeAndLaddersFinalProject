using System.Windows;
using System.Windows.Controls;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    /// <summary>
    /// Lógica de interacción para RankingPage.xaml
    /// </summary>
    public partial class RankingPage : Page
    {
        private readonly RankingViewModel _rankingViewModel;

        public RankingPage()
        {
            InitializeComponent();

            _rankingViewModel = new RankingViewModel();
            DataContext = _rankingViewModel;
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
