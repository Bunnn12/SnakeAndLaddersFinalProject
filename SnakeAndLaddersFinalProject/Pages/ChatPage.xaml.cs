using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ChatPage : Page
    {
        private ChatViewModel vm;

        public ChatPage(int lobbyId)
        {
            if (lobbyId <= 0) throw new ArgumentException("LobbyId inválido.", nameof(lobbyId));

            InitializeComponent();

            vm = new ChatViewModel(lobbyId);
            DataContext = vm;

            vm.Messages.CollectionChanged += Messages_CollectionChanged;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            vm?.Dispose();
        }

        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!vm.IsAutoScrollEnabled) return;
            if (lvMessages == null) return;
            if (lvMessages.Items.Count == 0) return;

            // Defer del scroll para evitar "ItemsControl no es coherente..."
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
    }
}
