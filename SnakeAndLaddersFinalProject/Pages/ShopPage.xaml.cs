using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.ShopService;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class ShopPage : Page
    {
        private const string ICON_TITLE_WARNING = "UiTitleWarning";
        private const string ICON_TITLE_ERROR = "UiTitleError";

        private const string SHOP_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IShopService";

        private const string SHOP_CODE_INVALID_SESSION = "SHOP_INVALID_SESSION";
        private const string SHOP_CODE_INVALID_USER_ID = "SHOP_INVALID_USER_ID";
        private const string SHOP_CODE_USER_NOT_FOUND = "SHOP_USER_NOT_FOUND";
        private const string SHOP_CODE_INSUFFICIENT_COINS = "SHOP_INSUFFICIENT_COINS";
        private const string SHOP_CODE_NO_AVATARS_FOR_RARITY = "SHOP_NO_AVATARS_FOR_RARITY";
        private const string SHOP_CODE_NO_STICKER_PACKS = "SHOP_NO_STICKER_PACKS";
        private const string SHOP_CODE_INVALID_DICE = "SHOP_INVALID_DICE_ID";
        private const string SHOP_CODE_DICE_NOT_FOUND = "SHOP_DICE_NOT_FOUND";
        private const string SHOP_CODE_ITEM_NOT_FOUND = "SHOP_ITEM_NOT_FOUND";
        private const string SHOP_CODE_SERVER_ERROR = "SHOP_SERVER_ERROR";

        private const int DICE_ID_NEGATIVE = 1;
        private const int DICE_ID_ONE_TO_THREE = 2;
        private const int DICE_ID_FOUR_TO_SIX = 3;

        private int currentCoins;

        public ShopPage()
        {
            InitializeComponent();
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            await InitializeCoinsAsync();
        }

        private async Task InitializeCoinsAsync()
        {
            lblCoinsValue.Text = "0";

            if (!EnsureAuthenticated())
            {
                return;
            }

            ShopServiceClient client = new ShopServiceClient(SHOP_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                string token = SessionContext.Current.AuthToken ?? string.Empty;

                int coins = await Task
                    .Run(() => client.GetCurrentCoins(token))
                    .ConfigureAwait(true);

                if (coins < 0)
                {
                    coins = 0;
                }

                currentCoins = coins;
                SessionContext.Current.Coins = coins;
                UpdateCoinsLabel();
            }
            catch (FaultException faultEx)
            {
                string mapped = MapShopCode(faultEx.Message);
                ShowError(mapped);
                client.Abort();
            }
            catch (EndpointNotFoundException)
            {
                ShowError(T("UiEndpointNotFound"));
                client.Abort();
            }
            catch (Exception)
            {
                ShowError(T("ShopServerError"));
                client.Abort();
            }
            finally
            {
                if (client.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }
        }

        private void UpdateCoinsLabel()
        {
            if (lblCoinsValue != null)
            {
                lblCoinsValue.Text = currentCoins.ToString();
            }
        }

        private static string T(string key)
        {
            return Globalization.LocalizationManager.Current[key];
        }

        private bool EnsureAuthenticated()
        {
            if (SessionContext.Current == null || !SessionContext.Current.IsAuthenticated)
            {
                ShowWarning(T("UiShopRequiresLogin"));
                return false;
            }

            if (SessionContext.Current.UserId <= 0)
            {
                ShowWarning(T("UiShopRequiresRegisteredUser"));
                return false;
            }

            if (string.IsNullOrWhiteSpace(SessionContext.Current.AuthToken))
            {
                ShowWarning(T("UiShopRequiresToken"));
                return false;
            }

            return true;
        }

        private void ShowWarning(string message)
        {
            MessageBox.Show(
                message,
                T(ICON_TITLE_WARNING),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(
                message,
                T(ICON_TITLE_ERROR),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(
                message,
                T("UiTitleInfo"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private string MapShopCode(string code)
        {
            switch (code)
            {
                case SHOP_CODE_INVALID_SESSION:
                    return T("ShopInvalidSessionError");

                case SHOP_CODE_INVALID_USER_ID:
                    return T("ShopInvalidUserIdError");

                case SHOP_CODE_USER_NOT_FOUND:
                    return T("ShopUserNotFoundError");

                case SHOP_CODE_INSUFFICIENT_COINS:
                    return T("ShopInsufficientCoinsWarn");

                case SHOP_CODE_NO_AVATARS_FOR_RARITY:
                    return T("ShopNoAvatarsForRarityWarn");

                case SHOP_CODE_NO_STICKER_PACKS:
                    return T("ShopNoStickerPacksWarn");

                case SHOP_CODE_INVALID_DICE:
                    return T("ShopInvalidDiceError");

                case SHOP_CODE_DICE_NOT_FOUND:
                    return T("ShopDiceNotFoundWarn");

                case SHOP_CODE_ITEM_NOT_FOUND:
                    return T("ShopItemNotFoundWarn");

                case SHOP_CODE_SERVER_ERROR:
                    return T("ShopServerError");

                default:
                    return T("ShopUnknownError");
            }
        }

        private async Task ExecutePurchaseAsync(Func<ShopServiceClient, ShopRewardDto> operation)
        {
            if (!EnsureAuthenticated())
            {
                return;
            }

            ShopServiceClient client = new ShopServiceClient(SHOP_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                ShopRewardDto reward = await Task
                    .Run(() => operation(client))
                    .ConfigureAwait(true);

                if (reward == null)
                {
                    ShowError(T("ShopServerError"));
                    return;
                }

                currentCoins = reward.CoinsAfter;
                SessionContext.Current.Coins = reward.CoinsAfter;
                UpdateCoinsLabel();

                string obtainedName = string.IsNullOrWhiteSpace(reward.RewardName)
                    ? "-"
                    : reward.RewardName;

                string message = string.Format(
                    T("UiShopPurchaseSuccessFmt"),
                    obtainedName,
                    currentCoins);

                ShowInfo(message);
            }
            catch (FaultException faultEx)
            {
                string mapped = MapShopCode(faultEx.Message);
                ShowError(mapped);
                client.Abort();
            }
            catch (EndpointNotFoundException)
            {
                ShowError(T("UiEndpointNotFound"));
                client.Abort();
            }
            catch (Exception)
            {
                ShowError(T("ShopServerError"));
                client.Abort();
            }
            finally
            {
                if (client.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }
        }

        // --- BUTTON HANDLERS COMPRAS ---

        private async void btnAvatarCommonBuy_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePurchaseAsync(client =>
                client.PurchaseAvatarChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Common));
        }

        private async void btnAvatarEpicBuy_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePurchaseAsync(client =>
                client.PurchaseAvatarChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Epic));
        }

        private async void btnAvatarLegendaryBuy_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePurchaseAsync(client =>
                client.PurchaseAvatarChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Legendary));
        }

        private async void btnStickerCommonBuy_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePurchaseAsync(client =>
                client.PurchaseStickerChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Common));
        }

        private async void btnStickerEpicBuy_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePurchaseAsync(client =>
                client.PurchaseStickerChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Epic));
        }

        private async void btnStickerLegendaryBuy_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePurchaseAsync(client =>
                client.PurchaseStickerChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Legendary));
        }

        private async void btnDiceNegativeBuy_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePurchaseAsync(client =>
                client.PurchaseDice(
                    SessionContext.Current.AuthToken,
                    DICE_ID_NEGATIVE));
        }

        private async void btnDice123Buy_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePurchaseAsync(client =>
                client.PurchaseDice(
                    SessionContext.Current.AuthToken,
                    DICE_ID_ONE_TO_THREE));
        }

        private async void btnDice456Buy_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePurchaseAsync(client =>
                client.PurchaseDice(
                    SessionContext.Current.AuthToken,
                    DICE_ID_FOUR_TO_SIX));
        }

        private async void btnItemChestBuy_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePurchaseAsync(client =>
                client.PurchaseItemChest(SessionContext.Current.AuthToken));
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
    }
}
