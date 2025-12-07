using System;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using SnakeAndLaddersFinalProject.Pages;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class ConnectionLostHandlerException
    {
        private const string CONNECTION_LOST_TITLE = "Conexión perdida";
        private const string CONNECTION_LOST_MESSAGE =
            "Se perdió la conexión con el servidor. Serás regresado a la pantalla de inicio.";

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
                        MessageBox.Show(
                            CONNECTION_LOST_MESSAGE,
                            CONNECTION_LOST_TITLE,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

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
