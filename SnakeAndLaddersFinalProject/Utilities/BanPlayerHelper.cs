using System.Windows;
using System.Windows.Controls;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Pages;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class BanPlayerHelper
    {
        private const string DEFAULT_BAN_MESSAGE = "Has sido baneado y expulsado del juego.";
        private const string BAN_TITLE = "Baneo";

        public static void HandleBanAndNavigateToLogin(Page currentPage, string message = null)
        {
            if (currentPage == null)
            {
                return;
            }

            string safeMessage = string.IsNullOrWhiteSpace(message)
                ? DEFAULT_BAN_MESSAGE
                : message.Trim();

            MessageBox.Show(
                safeMessage,
                BAN_TITLE,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            SessionContext.Current.UserId = 0;
            SessionContext.Current.UserName = string.Empty;
            SessionContext.Current.Email = string.Empty;
            SessionContext.Current.ProfilePhotoId = AvatarIdHelper.DEFAULT_AVATAR_ID;

            var currentWindow = Window.GetWindow(currentPage);
            var mainFrame = currentWindow?.FindName("MainFrame") as Frame;
            if (mainFrame != null)
            {
                mainFrame.Navigate(new LoginPage());
            }
        }
    }
}
