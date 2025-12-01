using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Pages;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ShopPage : Page
    {
        private ShopViewModel ViewModel
        {
            get { return DataContext as ShopViewModel; }
        }

        public ShopPage()
        {
            InitializeComponent();

            DataContext = new ShopViewModel();

            Loaded -= PageLoaded;
            Loaded += PageLoaded;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            lblCoinsValue.Text = "0";

            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.InitializeCoinsAsync();
            UpdateCoinsLabel();
        }

        private void UpdateCoinsLabel()
        {
            if (lblCoinsValue != null && ViewModel != null)
            {
                lblCoinsValue.Text = ViewModel.CurrentCoins.ToString();
            }
        }

        private static string T(string key)
        {
            return Globalization.LocalizationManager.Current[key];
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            Frame mainFrame = GetMainFrame();
            mainFrame?.Navigate(new SettingsPage());
        }

        private void btnNavFriends_Click(object sender, RoutedEventArgs e)
        {
            Frame mainFrame = GetMainFrame();
            mainFrame?.Navigate(new FriendsListPage());
        }

        private void btnNavInventory_Click(object sender, RoutedEventArgs e)
        {
            Frame mainFrame = GetMainFrame();
            mainFrame?.Navigate(new InventoryPage());
        }

        private void btnNavMain_Click(object sender, RoutedEventArgs e)
        {
            Frame mainFrame = GetMainFrame();
            mainFrame?.Navigate(new MainPage());
        }

        private void btnNavSkins_Click(object sender, RoutedEventArgs e)
        {
            Frame mainFrame = GetMainFrame();
            mainFrame?.Navigate(new SkinsPage());
        }

        private void btnNavProfile_Click(object sender, RoutedEventArgs e)
        {
            Frame mainFrame = GetMainFrame();
            mainFrame?.Navigate(new ProfilePage());
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

        private void btnAvatarCommonInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopAvatarCommonInfo");
        }

        private void btnAvatarEpicInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopAvatarEpicInfo");
        }

        private void btnAvatarLegendaryInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopAvatarLegendaryInfo");
        }

        private void btnStickerCommonInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopStickerCommonInfo");
        }

        private void btnStickerEpicInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopStickerEpicInfo");
        }

        private void btnStickerLegendaryInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopStickerLegendaryInfo");
        }

        private void btnDiceNegativeInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopDiceNegativeInfo");
        }

        private void btnDice123Info_Click(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopDice123Info");
        }

        private void btnDice456Info_Click(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopDice456Info");
        }

        private void btnItemChestInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowChestInfo("ShopItemChestInfo");
        }

        private async void btnAvatarCommonBuy_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.PurchaseAvatarCommonAsync();
            UpdateCoinsLabel();
        }

        private async void btnAvatarEpicBuy_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.PurchaseAvatarEpicAsync();
            UpdateCoinsLabel();
        }

        private async void btnAvatarLegendaryBuy_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.PurchaseAvatarLegendaryAsync();
            UpdateCoinsLabel();
        }

        private async void btnStickerCommonBuy_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.PurchaseStickerCommonAsync();
            UpdateCoinsLabel();
        }

        private async void btnStickerEpicBuy_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.PurchaseStickerEpicAsync();
            UpdateCoinsLabel();
        }

        private async void btnStickerLegendaryBuy_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.PurchaseStickerLegendaryAsync();
            UpdateCoinsLabel();
        }

        private async void btnDiceNegativeBuy_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.PurchaseDiceNegativeAsync();
            UpdateCoinsLabel();
        }

        private async void btnDice123Buy_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.PurchaseDiceOneToThreeAsync();
            UpdateCoinsLabel();
        }

        private async void btnDice456Buy_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.PurchaseDiceFourToSixAsync();
            UpdateCoinsLabel();
        }

        private async void btnItemChestBuy_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            await ViewModel.PurchaseItemChestAsync();
            UpdateCoinsLabel();
        }
    }
}
