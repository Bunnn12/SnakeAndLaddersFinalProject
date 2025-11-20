using System.Windows;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class OtherReportCommentWindow : Window
    {
        private const int MIN_COMMENT_LENGTH = 5;
        private const int MAX_COMMENT_LENGTH = 100;

        private const string MIN_LENGTH_MESSAGE_TEXT_KEY = "Properties.Langs.Lang.OtherReasonMinLengthMessage";

        public string ReportComment { get; private set; }

        public OtherReportCommentWindow()
        {
            InitializeComponent();

            TextBoxCharCounterHelper.AttachCounter(
                txtComment,
                lblCharCount,
                MAX_COMMENT_LENGTH);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string commentText = txtComment.Text;

            if (string.IsNullOrWhiteSpace(commentText) ||
                commentText.Trim().Length < MIN_COMMENT_LENGTH)
            {
                MessageBox.Show(
                    MIN_LENGTH_MESSAGE_TEXT_KEY,
                    Properties.Langs.Lang.OtherReasonWindowTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            ReportComment = commentText.Trim();
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
