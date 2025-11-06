using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class OtherReportCommentWindow : Window
    {
        private const int MIN_COMMENT_LENGTH = 5;

        public string ReportComment { get; private set; }

        public OtherReportCommentWindow()
        {
            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string comment = txtComment.Text;

            if (string.IsNullOrWhiteSpace(comment) ||
                comment.Trim().Length < MIN_COMMENT_LENGTH)
            {
                MessageBox.Show(
                    "Properties.Langs.Lang.OtherReasonMinLengthMessage",
                    Properties.Langs.Lang.OtherReasonWindowTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            ReportComment = comment.Trim();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
