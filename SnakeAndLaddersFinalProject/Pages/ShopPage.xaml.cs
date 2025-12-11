using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ShopPage : Page
    {
        private const string INITIAL_COINS_TEXT = "0";

        private ShopViewModel ShopViewModelInstance
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
            lblCoinsValue.Text = INITIAL_COINS_TEXT;

            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.InitializeCoinsAsync();
            UpdateCoinsLabel();
        }

        private void UpdateCoinsLabel()
        {
            if (lblCoinsValue != null && ShopViewModelInstance != null)
            {
                lblCoinsValue.Text = ShopViewModelInstance.CurrentCoins.ToString();
            }
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
            string message = Lang.ResourceManager.GetString(langKey);
            MessageBox.Show(
                message,
                Lang.UiShopChestInfoTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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
            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.PurchaseAvatarCommonAsync();
            UpdateCoinsLabel();
        }

        private async void AvatarEpicBuy(object sender, RoutedEventArgs e)
        {
            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.PurchaseAvatarEpicAsync();
            UpdateCoinsLabel();
        }

        private async void AvatarLegendaryBuy(object sender, RoutedEventArgs e)
        {
            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.PurchaseAvatarLegendaryAsync();
            UpdateCoinsLabel();
        }

        private async void StickerCommonBuy(object sender, RoutedEventArgs e)
        {
            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.PurchaseStickerCommonAsync();
            UpdateCoinsLabel();
        }

        private async void StickerEpicBuy(object sender, RoutedEventArgs e)
        {
            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.PurchaseStickerEpicAsync();
            UpdateCoinsLabel();
        }

        private async void StickerLegendaryBuy(object sender, RoutedEventArgs e)
        {
            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.PurchaseStickerLegendaryAsync();
            UpdateCoinsLabel();
        }

        private async void DiceNegativeBuy(object sender, RoutedEventArgs e)
        {
            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.PurchaseDiceNegativeAsync();
            UpdateCoinsLabel();
        }

        private async void Dice123Buy(object sender, RoutedEventArgs e)
        {
            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.PurchaseDiceOneToThreeAsync();
            UpdateCoinsLabel();
        }

        private async void Dice456Buy(object sender, RoutedEventArgs e)
        {
            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.PurchaseDiceFourToSixAsync();
            UpdateCoinsLabel();
        }

        private async void ItemChestBuy(object sender, RoutedEventArgs e)
        {
            if (ShopViewModelInstance == null)
            {
                return;
            }

            await ShopViewModelInstance.PurchaseItemChestAsync();
            UpdateCoinsLabel();
        }
    }
}
