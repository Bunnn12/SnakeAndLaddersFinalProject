using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Pages;
using System.Windows.Controls;
using System.Windows;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class BanPlayerHelper
    {
        private const string DEFAULT_BAN_MESSAGE = "Has sido baneado y expulsado del juego.";

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
                "Baneo",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            SessionContext.Current.UserId = 0;
            SessionContext.Current.UserName = string.Empty;
            SessionContext.Current.Email = string.Empty;
            SessionContext.Current.ProfilePhotoId = AvatarIdHelper.DefaultId;

            var currentWindow = Window.GetWindow(currentPage);
            var mainFrame = currentWindow?.FindName("MainFrame") as Frame;
            if (mainFrame != null)
            {
                mainFrame.Navigate(new LoginPage());
            }
        }
    }
}
