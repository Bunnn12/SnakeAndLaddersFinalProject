using System.Windows;
using System.Collections.ObjectModel;
using SnakeAndLaddersFinalProject.Models;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class StickerPickerWindow : Window
    {
        private static readonly ObservableCollection<StickerModel> _emptyCollection = new ObservableCollection<StickerModel>();
        public StickerModel SelectedSticker { get; private set; }

        public StickerPickerWindow(ObservableCollection<StickerModel> stickers)
        {
            InitializeComponent();
            ObservableCollection<StickerModel> safeStickers = stickers ?? _emptyCollection;
            icStickers.ItemsSource = safeStickers;
        }

        private void SelectSticker(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element))
            {
                return;
            }

            if (!(element.DataContext is StickerModel sticker))
            {
                return;
            }

            SelectedSticker = sticker;
            DialogResult = true;
        }
    }
}
