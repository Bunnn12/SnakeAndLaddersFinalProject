using System.Collections.Specialized;
using System.Windows.Controls;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ChatPage : Page
    {
        private ChatViewModel vm;

        public ChatPage()
        {
            InitializeComponent();
            vm = new ChatViewModel();
            DataContext = vm;

            vm.Messages.CollectionChanged += Messages_CollectionChanged;
        }

        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!vm.IsAutoScrollEnabled) return;
            if (lvMessages?.Items.Count > 0)
            {
                var last = lvMessages.Items[lvMessages.Items.Count - 1];
                lvMessages.ScrollIntoView(last);
            }
        }


    }
}
