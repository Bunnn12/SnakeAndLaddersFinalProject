using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.ShopService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ShopViewModel
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

        public int CurrentCoins { get; private set; }

        public ShopViewModel()
        {
            CurrentCoins = 0;
        }

        public async Task InitializeCoinsAsync()
        {
            CurrentCoins = 0;

            if (!EnsureAuthenticated())
            {
                return;
            }

            var client = new ShopServiceClient(SHOP_ENDPOINT_CONFIGURATION_NAME);

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

                CurrentCoins = coins;
                SessionContext.Current.Coins = coins;
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
                if (client.State == System.ServiceModel.CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }
        }

        public Task PurchaseAvatarCommonAsync()
        {
            return ExecutePurchaseAsync(
                client => client.PurchaseAvatarChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Common));
        }

        public Task PurchaseAvatarEpicAsync()
        {
            return ExecutePurchaseAsync(
                client => client.PurchaseAvatarChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Epic));
        }

        public Task PurchaseAvatarLegendaryAsync()
        {
            return ExecutePurchaseAsync(
                client => client.PurchaseAvatarChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Legendary));
        }

        public Task PurchaseStickerCommonAsync()
        {
            return ExecutePurchaseAsync(
                client => client.PurchaseStickerChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Common));
        }

        public Task PurchaseStickerEpicAsync()
        {
            return ExecutePurchaseAsync(
                client => client.PurchaseStickerChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Epic));
        }

        public Task PurchaseStickerLegendaryAsync()
        {
            return ExecutePurchaseAsync(
                client => client.PurchaseStickerChest(
                    SessionContext.Current.AuthToken,
                    ShopChestRarity.Legendary));
        }

        public Task PurchaseDiceNegativeAsync()
        {
            return ExecutePurchaseAsync(
                client => client.PurchaseDice(
                    SessionContext.Current.AuthToken,
                    DICE_ID_NEGATIVE));
        }

        public Task PurchaseDiceOneToThreeAsync()
        {
            return ExecutePurchaseAsync(
                client => client.PurchaseDice(
                    SessionContext.Current.AuthToken,
                    DICE_ID_ONE_TO_THREE));
        }

        public Task PurchaseDiceFourToSixAsync()
        {
            return ExecutePurchaseAsync(
                client => client.PurchaseDice(
                    SessionContext.Current.AuthToken,
                    DICE_ID_FOUR_TO_SIX));
        }

        public Task PurchaseItemChestAsync()
        {
            return ExecutePurchaseAsync(
                client => client.PurchaseItemChest(SessionContext.Current.AuthToken));
        }

        private async Task ExecutePurchaseAsync(Func<ShopServiceClient, ShopRewardDto> operation)
        {
            if (!EnsureAuthenticated())
            {
                return;
            }

            var client = new ShopServiceClient(SHOP_ENDPOINT_CONFIGURATION_NAME);

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

                CurrentCoins = reward.CoinsAfter;
                SessionContext.Current.Coins = reward.CoinsAfter;

                string obtainedName = string.IsNullOrWhiteSpace(reward.RewardName)
                    ? "-"
                    : reward.RewardName;

                string message = string.Format(
                    T("UiShopPurchaseSuccessFmt"),
                    obtainedName,
                    CurrentCoins);

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
                if (client.State == System.ServiceModel.CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }
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

        private static string T(string key)
        {
            return Globalization.LocalizationManager.Current[key];
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
    }
}
