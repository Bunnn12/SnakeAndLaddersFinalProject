using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
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

        private readonly ChatViewModel _chatViewModel;

        public ChatWindow(int lobbyId)
        {
            if (lobbyId <= 0)
            {
                throw new ArgumentException(INVALID_LOBBY_ID_MESSAGE, nameof(lobbyId));
            }

            InitializeComponent();

            _chatViewModel = new ChatViewModel(lobbyId);
            DataContext = _chatViewModel;

            _chatViewModel.Messages.CollectionChanged += MessagesCollectionChanged;
        }

        private async void ChatWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PositionRelativeToOwner();
            await InitializeChatAsync();
        }

        private void PositionRelativeToOwner()
        {
            Window ownerWindow = Owner;
            if (ownerWindow == null)
            {
                return;
            }

            Left = ownerWindow.Left + ownerWindow.Width - Width - WINDOW_MARGIN_PIXELS;
            Top = ownerWindow.Top + ownerWindow.Height - Height - WINDOW_MARGIN_PIXELS;
        }

        private async Task InitializeChatAsync()
        {
            await _chatViewModel.InitializeAsync();
        }

        private void WindowUnloaded(object sender, RoutedEventArgs e)
        {
            _chatViewModel?.Dispose();
        }

        private void MessagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_chatViewModel.IsAutoScrollEnabled)
            {
                return;
            }

            if (lvMessages == null || lvMessages.Items.Count == 0)
            {
                return;
            }

            Dispatcher.BeginInvoke(
                new Action(
                    () =>
                    {
                        try
                        {
                            lvMessages.UpdateLayout();
                            object lastItem = lvMessages.Items[lvMessages.Items.Count - 1];
                            lvMessages.ScrollIntoView(lastItem);

                            ScrollViewer scrollViewer = FindChild<ScrollViewer>(lvMessages);
                            scrollViewer?.ScrollToEnd();
                        }
                        catch
                        {
                            
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
                DependencyObject child = VisualTreeHelper.GetChild(parent, index);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                T nestedChild = FindChild<T>(child);
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
                new Action(
                    () =>
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
