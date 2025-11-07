using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using log4net;
using SnakeAndLaddersFinalProject.Mappers;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class ReportsWindow : Window
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ReportsWindow));

        private const int MIN_REGISTERED_USER_ID = 1;
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
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            if (!IsReportContextValid())
            {
                MessageBox.Show(
                    "Lang.ReportInvalidContextMessage",
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Close();
                return;
            }

            string reasonKey = button.Tag as string;
            if (string.IsNullOrWhiteSpace(reasonKey))
            {
                return;
            }

            if (string.Equals(reasonKey, "Other", StringComparison.OrdinalIgnoreCase))
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

            bool confirmed = ShowConfirmDialog(displayText);
            if (!confirmed)
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

            bool? result = commentWindow.ShowDialog();
            if (result != true)
            {
                return;
            }

            string customComment = commentWindow.ReportComment;
            if (string.IsNullOrWhiteSpace(customComment))
            {
                return;
            }

            bool confirmed = ShowConfirmDialog(customComment);
            if (!confirmed)
            {
                return;
            }

            SendReport(customComment);
            Close();
        }

        private static string ResolveDisplayText(string reasonKey)
        {
            if (string.Equals(reasonKey, "Harassment", StringComparison.OrdinalIgnoreCase))
            {
                return Lang.btnHarassmentText;
            }

            if (string.Equals(reasonKey, "InappropriateLanguage", StringComparison.OrdinalIgnoreCase))
            {
                return Lang.btnInappropiateLangText;
            }

            if (string.Equals(reasonKey, "ToxicBehavior", StringComparison.OrdinalIgnoreCase))
            {
                return Lang.btnToxicBehaviorText;
            }

            if (string.Equals(reasonKey, "Exploiting", StringComparison.OrdinalIgnoreCase))
            {
                return Lang.btnExploitingText;
            }

            return reasonKey;
        }

        private bool ShowConfirmDialog(string reasonText)
        {
            string targetName = string.IsNullOrWhiteSpace(ReportedUserName)
                ? "Lang.ReportUnknownUserDisplayName"
                : ReportedUserName;

            string message = string.Format(
                "Lang.ReportConfirmMessageFormat",
                targetName,
                reasonText);

            var result = MessageBox.Show(
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
                    "Lang.ReportSentSuccessfullyMessage",
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (FaultException<PlayerReportService.ServiceFault> fault)
            {
                string faultCode = fault.Detail != null ? fault.Detail.Code : null;
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
                Logger.Error("Endpoint de PlayerReportService no encontrado.", ex);

                MessageBox.Show(
                    "Lang.ReportEndpointNotFoundMessage",
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                client.Abort();
            }
            catch (Exception ex)
            {
                Logger.Error("Error inesperado al enviar el reporte.", ex);

                MessageBox.Show(
                    "Lang.ReportGenericErrorMessage",
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                client.Abort();
            }

        }
    }
}
