using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.ShopService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ShopViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ShopViewModel));

        private const string ICON_TITLE_WARNING = "UiTitleWarning";
        private const string ICON_TITLE_ERROR = "UiTitleError";
        private const string ICON_TITLE_INFO = "UiTitleInfo";

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

        private const string TOKEN_UI_SHOP_REQUIRES_LOGIN = "UiShopRequiresLogin";
        private const string TOKEN_UI_SHOP_REQUIRES_REGISTERED_USER = "UiShopRequiresRegisteredUser";
        private const string TOKEN_UI_SHOP_REQUIRES_TOKEN = "UiShopRequiresToken";
        private const string TOKEN_UI_ENDPOINT_NOT_FOUND = "UiEndpointNotFound";
        private const string TOKEN_UI_SHOP_PURCHASE_SUCCESS_FMT = "UiShopPurchaseSuccessFmt";

        private const string CONTEXT_INITIALIZE_COINS = "ShopViewModel.InitializeCoinsAsync";
        private const string CONTEXT_EXECUTE_PURCHASE = "ShopViewModel.ExecutePurchaseAsync";

        private const int DICE_ID_NEGATIVE = 1;
        private const int DICE_ID_ONE_TO_THREE = 2;
        private const int DICE_ID_FOUR_TO_SIX = 3;
        private const int MIN_COINS = 0;
        private const int INVALID_USER_ID = 0;

        private const string UNKNOWN_REWARD_PLACEHOLDER = "-";

        private int _currentCoins;

        public event PropertyChangedEventHandler PropertyChanged;

        public int CurrentCoins
        {
            get => _currentCoins;
            private set
            {
                if (_currentCoins == value)
                {
                    return;
                }

                _currentCoins = value;
                OnPropertyChanged();
            }
        }

        public ShopViewModel()
        {
            CurrentCoins = MIN_COINS;
        }

        public async Task InitializeCoinsAsync()
        {
            CurrentCoins = MIN_COINS;

            if (!EnsureAuthenticated())
            {
                return;
            }

            var client = new ShopServiceClient(SHOP_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                string token = SessionContext.Current.AuthToken ?? string.Empty;

                int coins = await Task.Run(() => client.GetCurrentCoins(token));

                if (coins < MIN_COINS)
                {
                    coins = MIN_COINS;
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
            catch (EndpointNotFoundException ex)
            {
                string message = ExceptionHandler.Handle(ex, CONTEXT_INITIALIZE_COINS, Logger);
                ShowError(message);
                client.Abort();
            }
            catch (Exception ex)
            {
                string message = ExceptionHandler.Handle(ex, CONTEXT_INITIALIZE_COINS, Logger);
                ShowError(message);
                client.Abort();
            }
            finally
            {
                SafeClose(client);
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
                ShopRewardDto reward = await Task.Run(() => operation(client));

                if (reward == null)
                {
                    ShowError(Token(SHOP_CODE_SERVER_ERROR));
                    return;
                }

                CurrentCoins = reward.CoinsAfter;
                SessionContext.Current.Coins = reward.CoinsAfter;

                string obtainedName = string.IsNullOrWhiteSpace(reward.RewardName)
                    ? UNKNOWN_REWARD_PLACEHOLDER
                    : reward.RewardName;

                string message = string.Format(
                    Token(TOKEN_UI_SHOP_PURCHASE_SUCCESS_FMT),
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
            catch (EndpointNotFoundException ex)
            {
                string message = ExceptionHandler.Handle(ex, CONTEXT_EXECUTE_PURCHASE, Logger);
                ShowError(message);
                client.Abort();
            }
            catch (Exception ex)
            {
                string message = ExceptionHandler.Handle(ex, CONTEXT_EXECUTE_PURCHASE, Logger);
                ShowError(message);
                client.Abort();
            }
            finally
            {
                SafeClose(client);
            }
        }

        private static bool EnsureAuthenticated()
        {
            if (SessionContext.Current == null || !SessionContext.Current.IsAuthenticated)
            {
                ShowWarning(Token(TOKEN_UI_SHOP_REQUIRES_LOGIN));
                return false;
            }

            if (SessionContext.Current.UserId <= INVALID_USER_ID)
            {
                ShowWarning(Token(TOKEN_UI_SHOP_REQUIRES_REGISTERED_USER));
                return false;
            }

            if (string.IsNullOrWhiteSpace(SessionContext.Current.AuthToken))
            {
                ShowWarning(Token(TOKEN_UI_SHOP_REQUIRES_TOKEN));
                return false;
            }

            return true;
        }

        private static void SafeClose(ShopServiceClient client)
        {
            if (client == null)
            {
                return;
            }

            try
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
            catch
            {
                client.Abort();
            }
        }

        private static string Token(string key)
        {
            return Globalization.LocalizationManager.Current[key];
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(
                message,
                Token(ICON_TITLE_WARNING),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(
                message,
                Token(ICON_TITLE_ERROR),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static void ShowInfo(string message)
        {
            MessageBox.Show(
                message,
                Token(ICON_TITLE_INFO),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static string MapShopCode(string code)
        {
            switch (code)
            {
                case SHOP_CODE_INVALID_SESSION:
                    return Token("ShopInvalidSessionError");

                case SHOP_CODE_INVALID_USER_ID:
                    return Token("ShopInvalidUserIdError");

                case SHOP_CODE_USER_NOT_FOUND:
                    return Token("ShopUserNotFoundError");

                case SHOP_CODE_INSUFFICIENT_COINS:
                    return Token("ShopInsufficientCoinsWarn");

                case SHOP_CODE_NO_AVATARS_FOR_RARITY:
                    return Token("ShopNoAvatarsForRarityWarn");

                case SHOP_CODE_NO_STICKER_PACKS:
                    return Token("ShopNoStickerPacksWarn");

                case SHOP_CODE_INVALID_DICE:
                    return Token("ShopInvalidDiceError");

                case SHOP_CODE_DICE_NOT_FOUND:
                    return Token("ShopDiceNotFoundWarn");

                case SHOP_CODE_ITEM_NOT_FOUND:
                    return Token("ShopItemNotFoundWarn");

                case SHOP_CODE_SERVER_ERROR:
                    return Token("ShopServerError");

                default:
                    return Token("ShopUnknownError");
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?
                .Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
