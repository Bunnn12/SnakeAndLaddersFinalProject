using System.Windows;
using System.Collections.ObjectModel;
using SnakeAndLaddersFinalProject.Models;

namespace SnakeAndLaddersFinalProject.Windows
{
    public partial class StickerPickerWindow : Window
    {
        private static readonly ObservableCollection<StickerModel> _emptyCollection =
            new ObservableCollection<StickerModel>();

        public StickerModel SelectedSticker { get; private set; }

        public StickerPickerWindow(ObservableCollection<StickerModel> stickers)
        {
            InitializeComponent();

            ObservableCollection<StickerModel> safeStickers =
                stickers ?? _emptyCollection;

            icStickers.ItemsSource = safeStickers;
        }

        private void StickerButton_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }

            StickerModel sticker = element.DataContext as StickerModel;
            if (sticker == null)
            {
                return;
            }

            SelectedSticker = sticker;
            DialogResult = true;
        }
    }
}
