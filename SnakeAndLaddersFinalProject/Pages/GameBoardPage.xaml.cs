using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using log4net;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class GameBoardPage : Page
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GameBoardPage));
        private GameBoardViewModel _currentViewModel;

        public GameBoardPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        public GameBoardViewModel ViewModel
        {
            get { return DataContext as GameBoardViewModel; }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            AttachToViewModel(ViewModel);

            if (_currentViewModel == null)
            {
                _logger.Warn("GameBoardPage.OnLoaded: shopViewModelInstance is null.");
                return;
            }

            try
            {
                if (_currentViewModel.Inventory != null)
                {
                    await _currentViewModel.Inventory.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error charging the inventory on the GameBoardPage.", ex);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachFromViewModel();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachFromViewModel();
            AttachToViewModel(e.NewValue as GameBoardViewModel);
        }

        private void AttachToViewModel(GameBoardViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            if (ReferenceEquals(_currentViewModel, viewModel))
            {
                return;
            }

            _currentViewModel = viewModel;
            _currentViewModel.PodiumRequested += OnPodiumRequested;

            _logger.Info("GameBoardPage: subscribed to PodiumRequested.");
        }

        private void DetachFromViewModel()
        {
            if (_currentViewModel == null)
            {
                return;
            }

            GameBoardViewModel viewModel = _currentViewModel;
            _currentViewModel = null;

            viewModel.PodiumRequested -= OnPodiumRequested;

            _logger.Info("GameBoardPage: disuscribed shopViewModelInstance events. " +
                "calling to Dispose().");

            if (viewModel is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Error("Error doing Dispose() of GameBoardViewModel.", ex);
                }
            }
        }

        private void OnPodiumRequested(PodiumViewModel podiumViewModel)
        {
            try
            {
                _logger.Info("OnPodiumRequested: recibided PodiumViewModel, " +
                    "navigating to PodiumPage.");

                if (podiumViewModel == null)
                {
                    _logger.Warn("OnPodiumRequested: podiumViewModel is null.");
                    return;
                }

                if (Application.Current == null || Application.Current.Dispatcher == null)
                {
                    _logger.Error("OnPodiumRequested: Application.Dispatcher is null, " +
                        "could not navigate.");
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    DetachFromViewModel();
                    PodiumPage podiumPage = new PodiumPage(podiumViewModel);

                    if (NavigationService != null)
                    {
                        NavigationService.Navigate(podiumPage);
                        return;
                    }

                    BasicWindow window = Window.GetWindow(this) as BasicWindow;
                    window?.MainFrame?.Navigate(podiumPage);
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Error al navegar a la página de podio desde PodiumRequested.", ex);
            }
        }

    }
}
