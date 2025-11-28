using log4net;
using SnakeAndLaddersFinalProject.Mappers;
using SnakeAndLaddersFinalProject.Pages;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Utilities;
using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class ReportsWindow : Window
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ReportsWindow));

        private const int MIN_REGISTERED_USER_ID = 1;

        private const string REASON_KEY_OTHER = "Other";
        private const string REASON_KEY_HARASSMENT = "Harassment";
        private const string REASON_KEY_INAPPROPRIATE_LANGUAGE = "InappropriateLanguage";
        private const string REASON_KEY_TOXIC_BEHAVIOR = "ToxicBehavior";
        private const string REASON_KEY_EXPLOITING = "Exploiting";

        private const string REPORT_INVALID_CONTEXT_MESSAGE_TEXT_KEY = "Lang.ReportInvalidContextMessage";
        private const string REPORT_UNKNOWN_USER_DISPLAY_NAME_TEXT_KEY = "Lang.ReportUnknownUserDisplayName";
        private const string REPORT_CONFIRM_MESSAGE_FORMAT_TEXT_KEY = "Lang.ReportConfirmMessageFormat"; 
        private const string REPORT_SENT_SUCCESSFULLY_MESSAGE_TEXT_KEY = "Lang.ReportSentSuccessfullyMessage";
        private const string REPORT_ENDPOINT_NOT_FOUND_MESSAGE_TEXT_KEY = "Lang.ReportEndpointNotFoundMessage";
        private const string REPORT_GENERIC_ERROR_MESSAGE_TEXT_KEY = "Lang.ReportGenericErrorMessage";

        public int ReporterUserId { get; set; }
        public int ReportedUserId { get; set; }
        public string ReportedUserName { get; set; }

        public ReportsWindow()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ReasonButton_Click(object sender, RoutedEventArgs e)
        {
            var reasonButton = sender as Button;
            if (reasonButton == null)
            {
                return;
            }

            if (!IsReportContextValid())
            {
                MessageBox.Show(
                    REPORT_INVALID_CONTEXT_MESSAGE_TEXT_KEY,
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Close();
                return;
            }

            var reasonKey = reasonButton.Tag as string;
            if (string.IsNullOrWhiteSpace(reasonKey))
            {
                return;
            }

            if (string.Equals(reasonKey, REASON_KEY_OTHER, StringComparison.OrdinalIgnoreCase))
            {
                HandleCustomReason();
            }
            else
            {
                HandlePredefinedReason(reasonKey);
            }
        }

        private bool IsReportContextValid()
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

        private void HandlePredefinedReason(string reasonKey)
        {
            string displayText = ResolveDisplayText(reasonKey);
            string internalText = displayText;

            bool isConfirmed = ShowConfirmDialog(displayText);
            if (!isConfirmed)
            {
                return;
            }

            SendReport(internalText);
            Close();
        }

        private void HandleCustomReason()
        {
            var commentWindow = new OtherReportCommentWindow
            {
                Owner = this
            };

            bool? dialogResult = commentWindow.ShowDialog();
            if (dialogResult != true)
            {
                return;
            }

            string customComment = commentWindow.ReportComment;
            if (string.IsNullOrWhiteSpace(customComment))
            {
                return;
            }

            bool isConfirmed = ShowConfirmDialog(customComment);
            if (!isConfirmed)
            {
                return;
            }

            SendReport(customComment);
            Close();
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
                ? REPORT_UNKNOWN_USER_DISPLAY_NAME_TEXT_KEY
                : ReportedUserName;
            string message = $"¿Confirma que desea reportar a **{targetName}** por el siguiente motivo: **{reasonText}**? ({"Lang.ReportConfirmMessageFormat"})";

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

            var client = new PlayerReportService.PlayerReportServiceClient("BasicHttpBinding_IPlayerReportService");

            try
            {
                client.CreateReport(reportDto);
                client.Close();

                MessageBox.Show(
                    REPORT_SENT_SUCCESSFULLY_MESSAGE_TEXT_KEY,
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (FaultException<PlayerReportService.ServiceFault> faultException)
            {
                string faultCode = faultException.Detail != null ? faultException.Detail.Code : null;
                string translated = PlayerReportErrorMapper.GetMessageForCode(faultCode);

                MessageBox.Show(
                    translated,
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                client.Abort();
            }
            
            catch (Exception ex)
            {
                String message = ExceptionHandler.Handle(
                        ex,
                        $"{nameof(ReportsWindow)}.{nameof(SendReport)}",
                        Logger);

                MessageBox.Show(
                    "ocurrio un error al mandar el reporte " + message,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                client.Abort();
            }
        }
    }
}