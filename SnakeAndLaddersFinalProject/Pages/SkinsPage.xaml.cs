using SnakeAndLaddersFinalProject.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class SkinsPage : Page
    {
        private readonly SkinsViewModel _viewModel;

        public SkinsPage()
        {
            InitializeComponent();

            _viewModel = new SkinsViewModel();
            DataContext = _viewModel;

            Loaded += SkinsPageLoaded;
        }

        private async void SkinsPageLoaded(object sender, RoutedEventArgs e)
        {
            await SafeLoadAsync();
        }

        private async Task SafeLoadAsync()
        {
            try
            {
                await _viewModel.LoadAsync();
            }
            catch
            {
                
            }
        }

        private void BackClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private async void ApplyClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.ApplySelectionAsync();
        }

        private void NextClick(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectNext();
        }

        private void PreviousClick(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectPrevious();
        }

        private void AvatarTileMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                _viewModel.SelectAvatarFromTile(element.DataContext);
            }
        }
    }
}
