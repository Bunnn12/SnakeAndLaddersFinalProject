using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class ExceptionHandler
    {

        private const string UI_KEY_GENERIC_ERROR = "UiGenericError";
        private const string UI_KEY_ENDPOINT_NOT_FOUND = "UiEndpointNotFound";
        private const string UI_KEY_TIMEOUT = "UiTimeout";
        private const string UI_KEY_COMMUNICATION_ERROR = "UiCommunicationError";
        private const string UI_KEY_NETWORK_ERROR = "UiNetworkError";
        private const string UI_KEY_SECURITY_ERROR = "UiSecurityError";
        private const string UI_KEY_INVALID_INPUT = "UiInvalidInput";
        private const string UI_KEY_OPERATION_CANCELED = "UiOperationCanceled";
        private const string UI_KEY_IO_ERROR = "UiIoError";
        private const string UI_KEY_SERVICE_ERROR = "UiServiceError";

        public static string Handle(Exception ex, string operationName, ILog logger)
        {
            if (ex == null)
            {
                return GetText(UI_KEY_GENERIC_ERROR);
            }

            if (logger != null)
            {
                LogException(ex, operationName, logger);
            }

            string uiKey = MapExceptionToUiKey(ex);
            return GetText(uiKey);
        }

        private static void LogException(Exception ex, string operationName, ILog logger)
        {
            string message = string.Format("Error in '{0}'.", operationName);

            if (ex is ArgumentException || ex is FormatException)
            {
                logger.WarnFormat(
                    "Validation error in '{0}'. Message={1}",
                    operationName,
                    ex.Message);
                return;
            }

            if (ex is TimeoutException || ex is TaskCanceledException || ex is OperationCanceledException)
            {
                logger.WarnFormat(
                    "Timeout / cancelled in '{0}'. Message={1}",
                    operationName,
                    ex.Message);
                return;
            }

            if (ex is EndpointNotFoundException || ex is CommunicationException || ex is SocketException)
            {
                logger.Error(message + " Communication error.", ex);
                return;
            }

            if (ex is MessageSecurityException || ex is SecurityException)
            {
                logger.Error(message + " Security error.", ex);
                return;
            }

            if (ex is IOException)
            {
                logger.Error(message + " IO error.", ex);
                return;
            }

            if (ex is ObjectDisposedException || ex is InvalidOperationException)
            {
                logger.Error(message + " Invalid object state.", ex);
                return;
            }

            if (ex is FaultException)
            {
                logger.Error(message + " Service fault.", ex);
                return;
            }

            logger.Error(message + " Unexpected error.", ex);
        }

        private static string MapExceptionToUiKey(Exception ex)
        {

            if (ex is ArgumentException || ex is FormatException)
            {
                return UI_KEY_INVALID_INPUT;
            }

            if (ex is TimeoutException)
            {
                return UI_KEY_TIMEOUT;
            }

            if (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                return UI_KEY_OPERATION_CANCELED;
            }

            if (ex is EndpointNotFoundException)
            {
                return UI_KEY_ENDPOINT_NOT_FOUND;
            }

            if (ex is CommunicationException)
            {
                return UI_KEY_COMMUNICATION_ERROR;
            }

            if (ex is SocketException)
            {
                return UI_KEY_NETWORK_ERROR;
            }

            if (ex is MessageSecurityException || ex is SecurityException)
            {
                return UI_KEY_SECURITY_ERROR;
            }

            if (ex is IOException)
            {
                return UI_KEY_IO_ERROR;
            }

            if (ex is FaultException)
            {
                return UI_KEY_SERVICE_ERROR;
            }

            return UI_KEY_GENERIC_ERROR;
        }

        private static string GetText(string key)
        {
            return Globalization.LocalizationManager.Current[key];
        }
    }
}
