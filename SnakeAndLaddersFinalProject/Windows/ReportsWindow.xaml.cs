using log4net;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class ReportsWindow : Window
    {
        private const string REASON_KEY_OTHER = "Other";

        private const string REPORT_INVALID_CONTEXT_MESSAGE_TEXT_KEY = "Lang.ReportInvalidContextMessage";

        private ReportsViewModel ViewModel
        {
            get { return DataContext as ReportsViewModel; }
        }

        public int ReporterUserId
        {
            get { return ViewModel != null ? ViewModel.ReporterUserId : 0; }
            set
            {
                if (ViewModel != null)
                {
                    ViewModel.ReporterUserId = value;
                }
            }
        }

        public int ReportedUserId
        {
            get { return ViewModel != null ? ViewModel.ReportedUserId : 0; }
            set
            {
                if (ViewModel != null)
                {
                    ViewModel.ReportedUserId = value;
                }
            }
        }

        public string ReportedUserName
        {
            get { return ViewModel != null ? ViewModel.ReportedUserName : null; }
            set
            {
                if (ViewModel != null)
                {
                    ViewModel.ReportedUserName = value;
                }
            }
        }

        public ReportsWindow()
        {
            InitializeComponent();

            DataContext = new ReportsViewModel();
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

            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            if (!viewModel.IsReportContextValid())
            {
                MessageBox.Show(
                    REPORT_INVALID_CONTEXT_MESSAGE_TEXT_KEY,
                    Lang.reportUserTittle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Close();
                return;
            }

            string reasonKey = reasonButton.Tag as string;
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
                bool sent = viewModel.HandlePredefinedReason(reasonKey);
                if (sent)
                {
                    Close();
                }
            }
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

            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            bool sent = viewModel.HandleCustomReason(customComment);
            if (sent)
            {
                Close();
            }
        }
    }
}
