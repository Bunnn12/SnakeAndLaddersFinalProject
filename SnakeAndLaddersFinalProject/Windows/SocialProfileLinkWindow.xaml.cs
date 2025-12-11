using System;
using System.Windows;
using SnakeAndLaddersFinalProject.SocialProfileService;
using Lang = SnakeAndLaddersFinalProject.Properties.Langs.Lang;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class SocialProfileLinkWindow : Window
    {
        public string ProfileLink { get; private set; }

        public SocialProfileLinkWindow(SocialNetworkType network)
        {
            InitializeComponent();

            switch (network)
            {
                case SocialNetworkType.Instagram:
                    lblTitle.Text = Lang.SocialProfileLinkInstagramTitle;
                    lblUserCaption.Text = Lang.SocialProfileLinkInstagramCaption;
                    break;
                case SocialNetworkType.Facebook:
                    lblTitle.Text = Lang.SocialProfileLinkFacebookTitle;
                    lblUserCaption.Text = Lang.SocialProfileLinkFacebookCaption;
                    break;
                case SocialNetworkType.Twitter:
                    lblTitle.Text = Lang.SocialProfileLinkTwitterTitle;
                    lblUserCaption.Text = Lang.SocialProfileLinkTwitterCaption;
                    break;
                default:
                    lblTitle.Text = Lang.SocialProfileLinkGenericTitle;
                    lblUserCaption.Text = Lang.SocialProfileLinkGenericCaption;
                    break;
            }
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            string value = (txtProfileLink.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show(
                    Lang.SocialProfileLinkUrlRequiredText,
                    Lang.UiTitleWarning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ProfileLink = value;
            DialogResult = true;
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
