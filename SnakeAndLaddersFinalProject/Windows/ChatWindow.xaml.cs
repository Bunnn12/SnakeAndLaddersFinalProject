using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using SnakeAndLaddersFinalProject.ViewModels;
using System.Windows.Threading;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class ChatWindow : Window
    {
        private ChatViewModel vm;

        public ChatWindow(int lobbyId)
        {
            if (lobbyId <= 0) throw new ArgumentException("LobbyId inválido.", nameof(lobbyId));

            InitializeComponent();

            vm = new ChatViewModel(lobbyId);
            DataContext = vm;

            vm.Messages.CollectionChanged += MessagesCollectionChanged;
        }
        private void ChatWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var ownerWindow = Owner;

            if (ownerWindow == null)
            {
                return;
            }

            const int margin = 16;

            Left = ownerWindow.Left + ownerWindow.Width - Width - margin;
            Top = ownerWindow.Top + ownerWindow.Height - Height - margin;
        }
        private void WindowUnloaded(object sender, RoutedEventArgs e)
        {
            vm?.Dispose();
        }

        private void MessagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!vm.IsAutoScrollEnabled) return;
            if (lvMessages == null) return;
            if (lvMessages.Items.Count == 0) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    lvMessages.UpdateLayout();
                    var last = lvMessages.Items[lvMessages.Items.Count - 1];
                    lvMessages.ScrollIntoView(last);

                    var sv = FindChild<ScrollViewer>(lvMessages);
                    sv?.ScrollToEnd();
                }
                catch { /* swallow */ }
            }), DispatcherPriority.Background);
        }

        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0, n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i < n; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var sub = FindChild<T>(child);
                if (sub != null) return sub;
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
                DispatcherPriority.ContextIdle
            );
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

