using System;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using SnakeAndLaddersFinalProject.Pages;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class ConnectionLostHandlerException
    {
        private static bool _isHandlingConnectionLost;

        public static bool IsConnectionException(Exception ex)
        {
            if (ex == null)
            {
                return false;
            }

            if (ex is CommunicationException ||
                ex is TimeoutException ||
                ex is EndpointNotFoundException)
            {
                return true;
            }

            Exception inner = ex.InnerException;

            return inner is CommunicationException ||
                   inner is TimeoutException ||
                   inner is EndpointNotFoundException;
        }

        public static void HandleConnectionLost()
        {
            if (_isHandlingConnectionLost)
            {
                return;
            }

            _isHandlingConnectionLost = true;

            if (Application.Current == null || Application.Current.Dispatcher == null)
            {
                _isHandlingConnectionLost = false;
                return;
            }

            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    try
                    {
                        MessageBox.Show(Lang.ConnectionLostMessage, Lang.ConnectionLostTitle,
                            MessageBoxButton.OK, MessageBoxImage.Warning);

                        BasicWindow mainWindow = Application.Current
                            .Windows
                            .OfType<BasicWindow>()
                            .FirstOrDefault();

                        if (mainWindow?.MainFrame != null)
                        {
                            mainWindow.MainFrame.Navigate(new LoginPage());
                        }
                    }
                    finally
                    {
                        _isHandlingConnectionLost = false;
                    }
                });
        }
    }
}
