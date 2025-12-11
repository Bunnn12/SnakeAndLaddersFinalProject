using System;
using System.ServiceModel;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class UiExceptionHelper
    {
        public static void ShowModuleError(
            Exception ex,
            string operationName,
            ILog logger,
            string moduleErrorText)
        {
            string genericMessage = ExceptionHandler.Handle(ex, operationName, logger);

            string finalMessage = string.Format(
                "{0} {1}",
                genericMessage,
                moduleErrorText);

            MessageBox.Show(
                finalMessage,
                Lang.UiGenericErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            if (ConnectionLostHandlerException.IsConnectionException(ex) || ex is FaultException)
            {
                ConnectionLostHandlerException.HandleConnectionLost();
            }
        }
    }
}
