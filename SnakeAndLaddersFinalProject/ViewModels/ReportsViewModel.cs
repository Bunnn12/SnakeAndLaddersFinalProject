using System;
using System.ServiceModel;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.Mappers;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ReportsViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ReportsViewModel));

        private const int MIN_REGISTERED_USER_ID = 1;

        private const string REASON_KEY_HARASSMENT = "Harassment";
        private const string REASON_KEY_INAPPROPRIATE_LANGUAGE = "InappropriateLanguage";
        private const string REASON_KEY_TOXIC_BEHAVIOR = "ToxicBehavior";
        private const string REASON_KEY_EXPLOITING = "Exploiting";

        public int ReporterUserId { get; set; }

        public int ReportedUserId { get; set; }

        public string ReportedUserName { get; set; }

        public bool IsReportContextValid()
        {
            if (ReporterUserId < MIN_REGISTERED_USER_ID)
            {
                return false;
            }

            if (ReportedUserId < MIN_REGISTERED_USER_ID)
            {
                return false;
            }

            if (ReporterUserId == ReportedUserId)
            {
                return false;
            }

            return true;
        }

        public bool HandlePredefinedReason(string reasonKey)
        {
            string displayText = ResolveDisplayText(reasonKey);
            string internalText = displayText;

            bool isConfirmed = ShowConfirmDialog(displayText);
            if (!isConfirmed)
            {
                return false;
            }

            SendReport(internalText);
            return true;
        }

        public bool HandleCustomReason(string customComment)
        {
            if (string.IsNullOrWhiteSpace(customComment))
            {
                return false;
            }

            bool isConfirmed = ShowConfirmDialog(customComment);
            if (!isConfirmed)
            {
                return false;
            }

            SendReport(customComment);
            return true;
        }

        private static string ResolveDisplayText(string reasonKey)
        {
            if (string.Equals(reasonKey, REASON_KEY_HARASSMENT, StringComparison.OrdinalIgnoreCase))
            {
                return Lang.btnHarassmentText;
            }

            if (string.Equals(reasonKey, REASON_KEY_INAPPROPRIATE_LANGUAGE, StringComparison.OrdinalIgnoreCase))
            {
                return Lang.btnInappropiateLangText;
            }

            if (string.Equals(reasonKey, REASON_KEY_TOXIC_BEHAVIOR, StringComparison.OrdinalIgnoreCase))
            {
                return Lang.btnToxicBehaviorText;
            }

            if (string.Equals(reasonKey, REASON_KEY_EXPLOITING, StringComparison.OrdinalIgnoreCase))
            {
                return Lang.btnExploitingText;
            }

            return reasonKey;
        }

        private bool ShowConfirmDialog(string reasonText)
        {
            string targetName = string.IsNullOrWhiteSpace(ReportedUserName)
                ? Lang.ReportUnknownUserDisplayName
                : ReportedUserName;

            string message = string.Format(
                Lang.ReportConfirmMessageFormat,
                targetName,
                reasonText);

            MessageBoxResult result = MessageBox.Show(
                message,
                Lang.reportUserTittle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        private void SendReport(string reasonText)
        {
            var reportDto = new PlayerReportService.ReportDto
            {
                ReporterUserId = ReporterUserId,
                ReportedUserId = ReportedUserId,
                ReportReason = reasonText
            };

            var client = new PlayerReportService.PlayerReportServiceClient(
                "BasicHttpBinding_IPlayerReportService");

            try
            {
                client.CreateReport(reportDto);
                client.Close();

                MessageBox.Show(
                    Lang.ReportSentSuccessfullyMessage,
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (FaultException<PlayerReportService.ServiceFault> faultException)
            {
                string faultCode = faultException.Detail != null
                    ? faultException.Detail.Code
                    : null;

                string translated = PlayerReportErrorMapper.GetMessageForCode(faultCode);

                MessageBox.Show(
                    translated,
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                client.Abort();
            }
            catch (EndpointNotFoundException ex)
            {
                _logger.Error("Endpoint not found while sending report.", ex);

                MessageBox.Show(
                    Lang.ReportEndpointNotFoundMessage,
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                client.Abort();
            }
            catch (Exception ex)
            {
                string technicalMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(ReportsViewModel)}.{nameof(SendReport)}",
                    _logger);

                string userMessage = string.Format(
                    "{0} {1}",
                    Lang.ReportGenericErrorMessage,
                    technicalMessage);

                MessageBox.Show(
                    userMessage,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                client.Abort();
            }
        }
    }
}
