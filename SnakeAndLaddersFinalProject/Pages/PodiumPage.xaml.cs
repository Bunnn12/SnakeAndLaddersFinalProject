using System.Windows.Controls;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class PodiumPage : Page
    {
        public PodiumViewModel ViewModel
        {
            get { return DataContext as PodiumViewModel; }
        }

        public PodiumPage()
        {
            InitializeComponent();
        }

        public PodiumPage(PodiumViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
