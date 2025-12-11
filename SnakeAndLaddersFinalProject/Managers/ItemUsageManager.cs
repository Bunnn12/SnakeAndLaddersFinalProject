using log4net;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.ViewModels;
using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeAndLaddersFinalProject.Managers
{
    public sealed class ItemUsageManager
    {
        private const byte ITEM_SLOT_1 = 1;
        private const byte ITEM_SLOT_2 = 2;
        private const byte ITEM_SLOT_3 = 3;

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

        public ItemUsageManager(
            int gameId,
            int localUserId,
            InventoryViewModel inventory,
            Func<IGameplayClient> getGameplayClient,
            ILog logger,
            Func<bool> getIsUseItemInProgress,
            Action<bool> setIsUseItemInProgress,
            Func<bool> getIsTargetSelectionActive,
            Action<bool> setIsTargetSelectionActive,
            Func<byte?> getPendingItemSlotNumber,
            Action<byte?> setPendingItemSlotNumber,
            Action<string> setLastItemNotification,
            Func<Task> refreshInventoryAsync,
            Func<Task> syncGameStateAsync,
            Action raiseAllCanExecuteChanged)
        {
            _gameId = gameId;
            _localUserId = localUserId;
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _getGameplayClient = getGameplayClient ?? throw new ArgumentNullException(
                nameof(getGameplayClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _getIsUseItemInProgress = getIsUseItemInProgress ?? throw new ArgumentNullException(
                nameof(getIsUseItemInProgress));
            _setIsUseItemInProgress = setIsUseItemInProgress ?? throw new ArgumentNullException(
                nameof(setIsUseItemInProgress));
            _getIsTargetSelectionActive = getIsTargetSelectionActive ??
                throw new ArgumentNullException(nameof(getIsTargetSelectionActive));
            _setIsTargetSelectionActive = setIsTargetSelectionActive ??
                throw new ArgumentNullException(nameof(setIsTargetSelectionActive));
            _getPendingItemSlotNumber = getPendingItemSlotNumber ?? throw new ArgumentNullException(
                nameof(getPendingItemSlotNumber));
            _setPendingItemSlotNumber = setPendingItemSlotNumber ?? throw new ArgumentNullException(
                nameof(setPendingItemSlotNumber));
            _setLastItemNotification = setLastItemNotification ?? throw new ArgumentNullException(
                nameof(setLastItemNotification));
            _refreshInventoryAsync = refreshInventoryAsync ?? throw new ArgumentNullException(
                nameof(refreshInventoryAsync));
            _syncGameStateAsync = syncGameStateAsync ?? throw new ArgumentNullException(
                nameof(syncGameStateAsync));
            _raiseAllCanExecuteChanged = raiseAllCanExecuteChanged ??
                throw new ArgumentNullException(nameof(raiseAllCanExecuteChanged));
        }

        public Task PrepareItemTargetSelectionAsync(byte slotNumber,
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

        public async Task OnTargetUserSelectedAsync(
            int userId,
            string unknownErrorMessage,
            string useItemFailureMessagePrefix,
            string useItemUnexpectedErrorMessage,
            string gameWindowTitle)
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

            await UseItemAsync(pendingSlot.Value, userId, unknownErrorMessage,
                useItemFailureMessagePrefix, useItemUnexpectedErrorMessage, gameWindowTitle);
        }

        public void CancelItemUse(string itemUseCancelledMessage)
        {
            if (!_getIsTargetSelectionActive() && !_getPendingItemSlotNumber().HasValue)
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
            if (_inventory == null)
            {
                return false;
            }

            switch (slotNumber)
            {
                case ITEM_SLOT_1:
                    return _inventory.Slot1Item != null && _inventory.Slot1Item.Quantity > 0;
                case ITEM_SLOT_2:
                    return _inventory.Slot2Item != null && _inventory.Slot2Item.Quantity > 0;
                case ITEM_SLOT_3:
                    return _inventory.Slot3Item != null && _inventory.Slot3Item.Quantity > 0;
                default:
                    return false;
            }
        }

        private async Task UseItemAsync(
            byte slotNumber,
            int targetUserId,
            string unknownErrorMessage,
            string useItemFailureMessagePrefix,
            string useItemUnexpectedErrorMessage,
            string gameWindowTitle)
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
                int? targetUserIdOrNull = targetUserId <= 0 ? (int?)null : targetUserId;
                UseItemResponseDto response = await gameplayClient.UseItemAsync(_gameId,
                    _localUserId, slotNumber, targetUserIdOrNull).ConfigureAwait(false);

                if (response == null || !response.Success)
                {
                    string failureReason = response != null && !string.IsNullOrWhiteSpace(response.FailureReason)
                        ? response.FailureReason
                        : unknownErrorMessage;

                    _logger.Warn("UseItem failed: " + failureReason);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show(useItemFailureMessagePrefix + failureReason, gameWindowTitle,
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    return;
                }

                _logger.InfoFormat("UseItem OK. GameId={0}, UserId={1}, Slot={2}, TargetUserId={3}",
                    _gameId, _localUserId, slotNumber, targetUserIdOrNull);

                await _refreshInventoryAsync().ConfigureAwait(false);
                await _syncGameStateAsync().ConfigureAwait(false);
            }
            catch (FaultException faultEx)
            {
                string failureReason = string.IsNullOrWhiteSpace(faultEx.Message) ?
                    unknownErrorMessage : faultEx.Message;
                _logger.Warn("UseItem business error: " + failureReason, faultEx);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(useItemFailureMessagePrefix + failureReason, gameWindowTitle,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
            catch (Exception ex)
            {
                _logger.Error(useItemUnexpectedErrorMessage, ex);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(useItemUnexpectedErrorMessage, gameWindowTitle,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                _setIsUseItemInProgress(false);
                _raiseAllCanExecuteChanged();
            }
        }
    }
}
