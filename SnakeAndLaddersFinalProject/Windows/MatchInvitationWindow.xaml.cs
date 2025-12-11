using System;
using System.Windows;
using System.Windows.Input;
using log4net;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class MatchInvitationWindow : Window
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(MatchInvitationWindow));

        public MatchInvitationWindow(int lobbyId, string gameCode)
        {
            InitializeComponent();

            try
            {
                var viewModel = new MatchInvitationViewModel(lobbyId, gameCode);
                viewModel.RequestClose += OnViewModelRequestClose;
                viewModel.ShowMessageRequested += OnViewModelShowMessageRequested;

                DataContext = viewModel;
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(MatchInvitationWindow)}.ctor",
                    _logger);

                MessageBox.Show(
                    this,
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Close();
            }
        }

        private void OnViewModelShowMessageRequested(string message, string title, MessageBoxImage icon)
        {
            MessageBox.Show(
                this,
                message,
                title,
                MessageBoxButton.OK,
                icon);
        }

        private void OnViewModelRequestClose()
        {
            Close();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

    }
}
