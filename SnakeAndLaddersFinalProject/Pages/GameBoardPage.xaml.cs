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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardPage));

        private GameBoardViewModel currentViewModel;

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

            if (currentViewModel == null)
            {
                Logger.Warn("GameBoardPage.OnLoaded: ViewModel es null.");
                return;
            }

            try
            {
                if (currentViewModel.Inventory != null)
                {
                    await currentViewModel.Inventory.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error al inicializar el inventario en GameBoardPage.", ex);
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

            if (ReferenceEquals(currentViewModel, viewModel))
            {
                return;
            }

            currentViewModel = viewModel;

            currentViewModel.PodiumRequested += OnPodiumRequested;
            currentViewModel.NavigateToPodiumRequested += OnNavigateToPodiumRequested;

            Logger.Info("GameBoardPage: suscrito a PodiumRequested y NavigateToPodiumRequested.");
        }

        private void DetachFromViewModel()
        {
            if (currentViewModel == null)
            {
                return;
            }

            GameBoardViewModel vm = currentViewModel;
            currentViewModel = null;

            vm.PodiumRequested -= OnPodiumRequested;
            vm.NavigateToPodiumRequested -= OnNavigateToPodiumRequested;

            Logger.Info("GameBoardPage: desuscrito de eventos del ViewModel. Llamando a Dispose().");

            if (vm is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error al hacer Dispose() del GameBoardViewModel.", ex);
                }
            }
        }

        private void OnPodiumRequested(PodiumViewModel podiumViewModel)
        {
            try
            {
                Logger.Info("OnPodiumRequested: recibido PodiumViewModel, navegando a PodiumPage.");

                if (podiumViewModel == null)
                {
                    Logger.Warn("OnPodiumRequested: podiumViewModel es null.");
                    return;
                }

                if (Application.Current == null || Application.Current.Dispatcher == null)
                {
                    Logger.Error("OnPodiumRequested: Application.Dispatcher es null, no se puede navegar.");
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
                Logger.Error("Error al navegar a la página de podio desde PodiumRequested.", ex);
            }
        }

        private void OnNavigateToPodiumRequested(int gameId, int winnerUserId)
        {
            try
            {
                Logger.InfoFormat(
                    "OnNavigateToPodiumRequested: gameId={0}, winnerUserId={1}",
                    gameId,
                    winnerUserId);

                GameBoardViewModel vm = currentViewModel;

                string winnerName = vm != null
                    ? vm.ResolveUserDisplayName(winnerUserId)
                    : string.Format(Lang.PodiumDefaultPlayerNameFmt, winnerUserId);


                ReadOnlyCollection<PodiumPlayerViewModel> podiumPlayers =
                    vm != null
                        ? vm.BuildPodiumPlayers(winnerUserId)
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
                Logger.Error("Error al navegar a la página de podio.", ex);
            }
        }
    }
}
