using System;
using System.Windows;
using System.Windows.Controls;
using log4net;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using SnakeAndLaddersFinalProject.Windows;
using System.Collections.ObjectModel;
using SnakeAndLaddersFinalProject.Properties.Langs;

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
                _logger.Warn("GameBoardPage.OnLoaded: shopViewModelInstance es null.");
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
                _logger.Error("Error al inicializar el inventario en GameBoardPage.", ex);
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
            _currentViewModel.NavigateToPodiumRequested += OnNavigateToPodiumRequested;

            _logger.Info("GameBoardPage: suscrito a PodiumRequested y NavigateToPodiumRequested.");
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
            viewModel.NavigateToPodiumRequested -= OnNavigateToPodiumRequested;

            _logger.Info("GameBoardPage: desuscrito de eventos del shopViewModelInstance. Llamando a Dispose().");

            if (viewModel is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Error("Error al hacer Dispose() del GameBoardViewModel.", ex);
                }
            }
        }

        private void OnPodiumRequested(PodiumViewModel podiumViewModel)
        {
            try
            {
                _logger.Info("OnPodiumRequested: recibido PodiumViewModel, navegando a PodiumPage.");

                if (podiumViewModel == null)
                {
                    _logger.Warn("OnPodiumRequested: podiumViewModel es null.");
                    return;
                }

                if (Application.Current == null || Application.Current.Dispatcher == null)
                {
                    _logger.Error("OnPodiumRequested: Application.Dispatcher es null, no se puede navegar.");
                    return;
                }

                Application.Current.Dispatcher.Invoke(
                    () =>
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

        private void OnNavigateToPodiumRequested(int gameId, int winnerUserId)
        {
            try
            {
                _logger.InfoFormat(
                    "OnNavigateToPodiumRequested: _gameId={0}, winnerUserId={1}",
                    gameId,
                    winnerUserId);

                GameBoardViewModel gameBoardViewModel = _currentViewModel;

                string winnerName = gameBoardViewModel != null
                    ? gameBoardViewModel.ResolveUserDisplayName(winnerUserId)
                    : string.Format(Lang.PodiumDefaultPlayerNameFmt, winnerUserId);


                ReadOnlyCollection<PodiumPlayerViewModel> podiumPlayers =
                    gameBoardViewModel != null
                        ? gameBoardViewModel.BuildPodiumPlayers(winnerUserId)
                        : new ReadOnlyCollection<PodiumPlayerViewModel>(
                            new PodiumPlayerViewModel[0]);

                PodiumViewModel podiumViewModel = new PodiumViewModel();
                podiumViewModel.Initialize(winnerUserId, winnerName, podiumPlayers);

                DetachFromViewModel();

                PodiumPage podiumPage = new PodiumPage(podiumViewModel);

                if (NavigationService != null)
                {
                    NavigationService.Navigate(podiumPage);
                    return;
                }

                BasicWindow window = Window.GetWindow(this) as BasicWindow;
                window?.MainFrame?.Navigate(podiumPage);
            }
            catch (Exception ex)
            {
                _logger.Error("Error al navegar a la página de podio.", ex);
            }
        }
    }
}
