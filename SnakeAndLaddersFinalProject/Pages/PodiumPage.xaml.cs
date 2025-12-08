using System.Windows;
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
            PodiumViewModel vm = ViewModel;

            if (vm != null)
            {
                vm.CloseRequested = OnCloseRequested;
            }

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            PodiumViewModel viewModel = e.NewValue as PodiumViewModel;

            if (viewModel != null)
            {
                viewModel.CloseRequested = OnCloseRequested;
            }
        }

        private void OnCloseRequested()
        {
            Page mainPage = new MainPage();

            if (NavigationService != null)
            {
                NavigationService.Navigate(mainPage);
                return;
            }

            BasicWindow window = Window.GetWindow(this) as BasicWindow;
            window?.MainFrame?.Navigate(mainPage);
        }
    }
}
