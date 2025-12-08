using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ShopPage : Page
    {
        private ShopViewModel shopViewModelInstance
        {
            get { return DataContext as ShopViewModel; }
        }

        public ShopPage()
        {
            InitializeComponent();

            DataContext = new ShopViewModel();
            Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            lblCoinsValue.Text = "0";

            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.InitializeCoinsAsync();
            UpdateCoinsLabel();
        }

        private void UpdateCoinsLabel()
        {
            if (lblCoinsValue != null && shopViewModelInstance != null)
            {
                lblCoinsValue.Text = shopViewModelInstance.CurrentCoins.ToString();
            }
        }

        private static string T(string key)
        {
            return Globalization.LocalizationManager.Current[key];
        }

        private void Settings(object sender, RoutedEventArgs e)
        {
            Frame mainFrame = GetMainFrame();
            mainFrame?.Navigate(new SettingsPage());
        }

        private Frame GetMainFrame()
        {
            Window owner = Window.GetWindow(this) ?? Application.Current.MainWindow;
            return owner?.FindName("MainFrame") as Frame;
        }

        private void Back(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void ShowChestInfo(string langKey)
        {
            string message = T(langKey);
            MessageBox.Show(
                message,
                T("UiShopChestInfoTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void AvatarCommonInfo(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopAvatarCommonInfo");
        }

        private void AvatarEpicInfo(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopAvatarEpicInfo");
        }

        private void AvatarLegendaryInfo(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopAvatarLegendaryInfo");
        }

        private void StickerCommonInfo(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopStickerCommonInfo");
        }

        private void StickerEpicInfo(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopStickerEpicInfo");
        }

        private void StickerLegendaryInfo(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopStickerLegendaryInfo");
        }

        private void DiceNegativeInfo(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopDiceNegativeInfo");
        }

        private void Dice123Info(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopDice123Info");
        }

        private void Dice456Info(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopDice456Info");
        }

        private void ItemChestInfo(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopItemChestInfo");
        }

        private async void AvatarCommonBuy(object sender, RoutedEventArgs e)
        {
            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.PurchaseAvatarCommonAsync();
            UpdateCoinsLabel();
        }

        private async void AvatarEpicBuy(object sender, RoutedEventArgs e)
        {
            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.PurchaseAvatarEpicAsync();
            UpdateCoinsLabel();
        }

        private async void AvatarLegendaryBuy(object sender, RoutedEventArgs e)
        {
            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.PurchaseAvatarLegendaryAsync();
            UpdateCoinsLabel();
        }

        private async void StickerCommonBuy(object sender, RoutedEventArgs e)
        {
            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.PurchaseStickerCommonAsync();
            UpdateCoinsLabel();
        }

        private async void StickerEpicBuy(object sender, RoutedEventArgs e)
        {
            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.PurchaseStickerEpicAsync();
            UpdateCoinsLabel();
        }

        private async void StickerLegendaryBuy(object sender, RoutedEventArgs e)
        {
            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.PurchaseStickerLegendaryAsync();
            UpdateCoinsLabel();
        }

        private async void DiceNegativeBuy(object sender, RoutedEventArgs e)
        {
            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.PurchaseDiceNegativeAsync();
            UpdateCoinsLabel();
        }

        private async void Dice123Buy(object sender, RoutedEventArgs e)
        {
            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.PurchaseDiceOneToThreeAsync();
            UpdateCoinsLabel();
        }

        private async void Dice456Buy(object sender, RoutedEventArgs e)
        {
            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.PurchaseDiceFourToSixAsync();
            UpdateCoinsLabel();
        }

        private async void ItemChestBuy(object sender, RoutedEventArgs e)
        {
            if (shopViewModelInstance == null)
            {
                return;
            }

            await shopViewModelInstance.PurchaseItemChestAsync();
            UpdateCoinsLabel();
        }
    }
}
