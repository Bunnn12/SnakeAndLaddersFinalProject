using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Managers
{
    public sealed class ItemUsageManager
    {
        private const int MIN_GAME_ID = 1;
        private const int INVALID_USER_ID = 0;

        private const byte ITEM_SLOT_1 = 1;
        private const byte ITEM_SLOT_2 = 2;
        private const byte ITEM_SLOT_3 = 3;

        private const string LOG_USE_ITEM_FAILED_PREFIX = "UseItem failed: ";
        private const string LOG_USE_ITEM_OK_FORMAT =
            "UseItem OK. GameId={0}, UserId={1}, Slot={2}, TargetUserId={3}";
        private const string LOG_USE_ITEM_BUSINESS_ERROR_PREFIX =
            "UseItem business error: ";

        private readonly int _gameId;
        private readonly int _localUserId;

        private readonly InventoryViewModel _inventory;
        private readonly Func<IGameplayClient> _getGameplayClient;
        private readonly ILog _logger;

        private readonly Func<bool> _getIsUseItemInProgress;
        private readonly Action<bool> _setIsUseItemInProgress;
        private readonly Func<bool> _getIsTargetSelectionActive;
        private readonly Action<bool> _setIsTargetSelectionActive;
        private readonly Func<byte?> _getPendingItemSlotNumber;
        private readonly Action<byte?> _setPendingItemSlotNumber;
        private readonly Action<string> _setLastItemNotification;
        private readonly Func<Task> _refreshInventoryAsync;
        private readonly Func<Task> _syncGameStateAsync;
        private readonly Action _raiseAllCanExecuteChanged;

        private readonly string _unknownErrorMessage;
        private readonly string _useItemFailureMessagePrefix;
        private readonly string _useItemUnexpectedErrorMessage;
        private readonly string _gameWindowTitle;

        public ItemUsageManager(
            int gameId,
            int localUserId,
            ItemUsageManagerDependencies dependencies,
            ItemUsageMessages messages)
        {
            if (gameId < MIN_GAME_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            if (localUserId <= INVALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(localUserId));
            }

            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            if (dependencies.Inventory == null ||
                dependencies.GetGameplayClient == null ||
                dependencies.Logger == null ||
                dependencies.GetIsUseItemInProgress == null ||
                dependencies.SetIsUseItemInProgress == null ||
                dependencies.GetIsTargetSelectionActive == null ||
                dependencies.SetIsTargetSelectionActive == null ||
                dependencies.GetPendingItemSlotNumber == null ||
                dependencies.SetPendingItemSlotNumber == null ||
                dependencies.SetLastItemNotification == null ||
                dependencies.RefreshInventoryAsync == null ||
                dependencies.SyncGameStateAsync == null ||
                dependencies.RaiseAllCanExecuteChanged == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            if (messages.UnknownErrorMessage == null ||
                messages.UseItemFailureMessagePrefix == null ||
                messages.UseItemUnexpectedErrorMessage == null ||
                messages.GameWindowTitle == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            _gameId = gameId;
            _localUserId = localUserId;

            _inventory = dependencies.Inventory;
            _getGameplayClient = dependencies.GetGameplayClient;
            _logger = dependencies.Logger;

            _getIsUseItemInProgress = dependencies.GetIsUseItemInProgress;
            _setIsUseItemInProgress = dependencies.SetIsUseItemInProgress;

            _getIsTargetSelectionActive = dependencies.GetIsTargetSelectionActive;
            _setIsTargetSelectionActive = dependencies.SetIsTargetSelectionActive;

            _getPendingItemSlotNumber = dependencies.GetPendingItemSlotNumber;
            _setPendingItemSlotNumber = dependencies.SetPendingItemSlotNumber;

            _setLastItemNotification = dependencies.SetLastItemNotification;

            _refreshInventoryAsync = dependencies.RefreshInventoryAsync;
            _syncGameStateAsync = dependencies.SyncGameStateAsync;

            _raiseAllCanExecuteChanged = dependencies.RaiseAllCanExecuteChanged;

            _unknownErrorMessage = messages.UnknownErrorMessage;
            _useItemFailureMessagePrefix = messages.UseItemFailureMessagePrefix;
            _useItemUnexpectedErrorMessage = messages.UseItemUnexpectedErrorMessage;
            _gameWindowTitle = messages.GameWindowTitle;
        }


        public Task PrepareItemTargetSelectionAsync(
            byte slotNumber,
            string selectTargetPlayerMessage)
        {
            if (!HasItemInSlot(slotNumber))
            {
                return Task.CompletedTask;
            }

            _setPendingItemSlotNumber(slotNumber);
            _setIsTargetSelectionActive(true);
            _setLastItemNotification(selectTargetPlayerMessage);
            _raiseAllCanExecuteChanged();

            return Task.CompletedTask;
        }

        public async Task OnTargetUserSelectedAsync(int userId)
        {
            if (!_getIsTargetSelectionActive())
            {
                return;
            }

            byte? pendingSlot = _getPendingItemSlotNumber();
            if (!pendingSlot.HasValue)
            {
                return;
            }

            _setIsTargetSelectionActive(false);
            _setPendingItemSlotNumber(null);

            await UseItemAsync(pendingSlot.Value, userId).ConfigureAwait(false);
        }

        public void CancelItemUse(string itemUseCancelledMessage)
        {
            if (!_getIsTargetSelectionActive() &&
                !_getPendingItemSlotNumber().HasValue)
            {
                return;
            }

            _setPendingItemSlotNumber(null);
            _setIsTargetSelectionActive(false);
            _setLastItemNotification(itemUseCancelledMessage);
            _raiseAllCanExecuteChanged();
        }

        public bool HasItemInSlot(byte slotNumber)
        {
            switch (slotNumber)
            {
                case ITEM_SLOT_1:
                    return _inventory.Slot1Item != null &&
                           _inventory.Slot1Item.Quantity > 0;
                case ITEM_SLOT_2:
                    return _inventory.Slot2Item != null &&
                           _inventory.Slot2Item.Quantity > 0;
                case ITEM_SLOT_3:
                    return _inventory.Slot3Item != null &&
                           _inventory.Slot3Item.Quantity > 0;
                default:
                    return false;
            }
        }

        private async Task UseItemAsync(byte slotNumber, int targetUserId)
        {
            if (_getIsUseItemInProgress())
            {
                return;
            }

            IGameplayClient gameplayClient = _getGameplayClient();
            if (gameplayClient == null)
            {
                return;
            }

            _setIsUseItemInProgress(true);
            _raiseAllCanExecuteChanged();

            try
            {
                int? targetUserIdOrNull = targetUserId <= INVALID_USER_ID
                    ? (int?)null
                    : targetUserId;

                UseItemResponseDto response = await gameplayClient
                    .UseItemAsync(_gameId, _localUserId, slotNumber, targetUserIdOrNull)
                    .ConfigureAwait(false);

                if (!IsSuccessfulResponse(response))
                {
                    await HandleUseItemFailureAsync(response).ConfigureAwait(false);
                    return;
                }

                _logger.InfoFormat(
                    LOG_USE_ITEM_OK_FORMAT,
                    _gameId,
                    _localUserId,
                    slotNumber,
                    targetUserIdOrNull);

                await _refreshInventoryAsync().ConfigureAwait(false);
                await _syncGameStateAsync().ConfigureAwait(false);
            }
            catch (FaultException faultEx)
            {
                await HandleBusinessErrorAsync(faultEx).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await HandleUnexpectedErrorAsync(ex).ConfigureAwait(false);
            }
            finally
            {
                _setIsUseItemInProgress(false);
                _raiseAllCanExecuteChanged();
            }
        }

        private static bool IsSuccessfulResponse(UseItemResponseDto response)
        {
            return response != null && response.Success;
        }

        private async Task HandleUseItemFailureAsync(UseItemResponseDto response)
        {
            string failureReason = GetFailureReason(response);

            _logger.Warn(LOG_USE_ITEM_FAILED_PREFIX + failureReason);

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    MessageBox.Show(
                        _useItemFailureMessagePrefix + failureReason,
                        _gameWindowTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
        }

        private async Task HandleBusinessErrorAsync(FaultException faultEx)
        {
            string failureReason = string.IsNullOrWhiteSpace(faultEx.Message)
                ? _unknownErrorMessage
                : faultEx.Message;

            _logger.Warn(
                LOG_USE_ITEM_BUSINESS_ERROR_PREFIX + failureReason,
                faultEx);

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    MessageBox.Show(
                        _useItemFailureMessagePrefix + failureReason,
                        _gameWindowTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
        }

        private async Task HandleUnexpectedErrorAsync(Exception ex)
        {
            _logger.Error(_useItemUnexpectedErrorMessage, ex);

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    MessageBox.Show(
                        _useItemUnexpectedErrorMessage,
                        _gameWindowTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
        }

        private string GetFailureReason(UseItemResponseDto response)
        {
            if (response != null &&
                !string.IsNullOrWhiteSpace(response.FailureReason))
            {
                return response.FailureReason;
            }

            return _unknownErrorMessage;
        }
    }

    public sealed class ItemUsageManagerDependencies
    {
        public InventoryViewModel Inventory { get; set; }
        public Func<IGameplayClient> GetGameplayClient { get; set; }
        public ILog Logger { get; set; }

        public Func<bool> GetIsUseItemInProgress { get; set; }
        public Action<bool> SetIsUseItemInProgress { get; set; }

        public Func<bool> GetIsTargetSelectionActive { get; set; }
        public Action<bool> SetIsTargetSelectionActive { get; set; }

        public Func<byte?> GetPendingItemSlotNumber { get; set; }
        public Action<byte?> SetPendingItemSlotNumber { get; set; }

        public Action<string> SetLastItemNotification { get; set; }

        public Func<Task> RefreshInventoryAsync { get; set; }
        public Func<Task> SyncGameStateAsync { get; set; }

        public Action RaiseAllCanExecuteChanged { get; set; }
    }

    public sealed class ItemUsageMessages
    {
        public string UnknownErrorMessage { get; set; }
        public string UseItemFailureMessagePrefix { get; set; }
        public string UseItemUnexpectedErrorMessage { get; set; }
        public string GameWindowTitle { get; set; }
    }
}
