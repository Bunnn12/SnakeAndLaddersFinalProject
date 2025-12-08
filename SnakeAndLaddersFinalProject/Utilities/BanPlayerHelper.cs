using System.Windows;
using System.Windows.Controls;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Pages;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class BanPlayerHelper
    {
        public static void HandleBanAndNavigateToLogin(Page sourcePage, string banMessage = null)
        {
            if (sourcePage == null)
            {
                return;
            }

            string messageToShow = string.IsNullOrWhiteSpace(banMessage)
                 ? Lang.LobbyBannedAndKickedText
                 : banMessage.Trim();

            MessageBox.Show(messageToShow, Lang.UiTitleWarning,
               MessageBoxButton.OK, MessageBoxImage.Warning);

            SessionContext.Current.UserId = 0;
            SessionContext.Current.UserName = string.Empty;
            SessionContext.Current.Email = string.Empty;
            SessionContext.Current.ProfilePhotoId = AvatarIdHelper.DEFAULT_AVATAR_ID;

            Window ownerWindow = Window.GetWindow(sourcePage);
            Frame mainFrame = ownerWindow?.FindName("MainFrame") as Frame;
            if (mainFrame != null)
            {
                mainFrame.Navigate(new LoginPage());
            }
        }
    }
}
