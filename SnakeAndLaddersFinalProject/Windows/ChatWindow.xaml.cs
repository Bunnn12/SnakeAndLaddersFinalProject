using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class ChatWindow : Window
    {
        private const string INVALID_LOBBY_ID_MESSAGE = "LobbyId inválido.";
        private const int WINDOW_MARGIN_PIXELS = 16;

        private readonly ChatViewModel chatViewModel;

        public ChatWindow(int lobbyId)
        {
            if (lobbyId <= 0)
            {
                throw new ArgumentException(INVALID_LOBBY_ID_MESSAGE, nameof(lobbyId));
            }

            InitializeComponent();

            chatViewModel = new ChatViewModel(lobbyId);
            DataContext = chatViewModel;

            chatViewModel.Messages.CollectionChanged += MessagesCollectionChanged;
        }

        private void ChatWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var ownerWindow = Owner;
            if (ownerWindow == null)
            {
                return;
            }

            Left = ownerWindow.Left + ownerWindow.Width - Width - WINDOW_MARGIN_PIXELS;
            Top = ownerWindow.Top + ownerWindow.Height - Height - WINDOW_MARGIN_PIXELS;
        }

        private void WindowUnloaded(object sender, RoutedEventArgs e)
        {
            chatViewModel?.Dispose();
        }

        private void MessagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!chatViewModel.IsAutoScrollEnabled)
            {
                return;
            }

            if (lvMessages == null || lvMessages.Items.Count == 0)
            {
                return;
            }

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    try
                    {
                        lvMessages.UpdateLayout();
                        var lastItem = lvMessages.Items[lvMessages.Items.Count - 1];
                        lvMessages.ScrollIntoView(lastItem);

                        var scrollViewer = FindChild<ScrollViewer>(lvMessages);
                        scrollViewer?.ScrollToEnd();
                    }
                    catch
                    {
                        // Intencionalmente ignorado: si falla el autoscroll no debe romper la ventana de chat.
                    }
                }),
                DispatcherPriority.Background);
        }

        private static T FindChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int index = 0; index < childrenCount; index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var nestedChild = FindChild<T>(child);
                if (nestedChild != null)
                {
                    return nestedChild;
                }
            }

            return null;
        }

        private void TaMessage_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    taMessage.Focus();
                    Keyboard.Focus(taMessage);
                }),
                DispatcherPriority.ContextIdle);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
