using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.Windows;

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
            HookCloseRequested();
        }

        public PodiumPage(PodiumViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            HookCloseRequested();
        }

        private void HookCloseRequested()
        {
            var viewModel = ViewModel;
            if (viewModel != null)
            {
                viewModel.CloseRequested = OnCloseRequested;
            }

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = e.NewValue as PodiumViewModel;
            if (viewModel != null)
            {
                viewModel.CloseRequested = OnCloseRequested;
            }
        }

        private void OnCloseRequested()
        {
            var mainPage = new MainPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(mainPage);
                return;
            }

            var window = Window.GetWindow(this) as BasicWindow;
            window?.MainFrame?.Navigate(mainPage);
        }
    }
}
