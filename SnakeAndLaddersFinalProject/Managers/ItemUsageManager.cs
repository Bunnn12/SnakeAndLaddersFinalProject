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

        private readonly int gameId;
        private readonly int localUserId;
        private readonly InventoryViewModel inventory;
        private readonly Func<IGameplayClient> getGameplayClient;
        private readonly ILog logger;

        private readonly Func<bool> getIsUseItemInProgress;
        private readonly Action<bool> setIsUseItemInProgress;
        private readonly Func<bool> getIsTargetSelectionActive;
        private readonly Action<bool> setIsTargetSelectionActive;
        private readonly Func<byte?> getPendingItemSlotNumber;
        private readonly Action<byte?> setPendingItemSlotNumber;
        private readonly Action<string> setLastItemNotification;
        private readonly Func<Task> refreshInventoryAsync;
        private readonly Func<Task> syncGameStateAsync;
        private readonly Action raiseAllCanExecuteChanged;

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
            this.gameId = gameId;
            this.localUserId = localUserId;
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.getGameplayClient = getGameplayClient ?? throw new ArgumentNullException(nameof(getGameplayClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.getIsUseItemInProgress = getIsUseItemInProgress ?? throw new ArgumentNullException(nameof(getIsUseItemInProgress));
            this.setIsUseItemInProgress = setIsUseItemInProgress ?? throw new ArgumentNullException(nameof(setIsUseItemInProgress));
            this.getIsTargetSelectionActive = getIsTargetSelectionActive ?? throw new ArgumentNullException(nameof(getIsTargetSelectionActive));
            this.setIsTargetSelectionActive = setIsTargetSelectionActive ?? throw new ArgumentNullException(nameof(setIsTargetSelectionActive));
            this.getPendingItemSlotNumber = getPendingItemSlotNumber ?? throw new ArgumentNullException(nameof(getPendingItemSlotNumber));
            this.setPendingItemSlotNumber = setPendingItemSlotNumber ?? throw new ArgumentNullException(nameof(setPendingItemSlotNumber));
            this.setLastItemNotification = setLastItemNotification ?? throw new ArgumentNullException(nameof(setLastItemNotification));
            this.refreshInventoryAsync = refreshInventoryAsync ?? throw new ArgumentNullException(nameof(refreshInventoryAsync));
            this.syncGameStateAsync = syncGameStateAsync ?? throw new ArgumentNullException(nameof(syncGameStateAsync));
            this.raiseAllCanExecuteChanged = raiseAllCanExecuteChanged ?? throw new ArgumentNullException(nameof(raiseAllCanExecuteChanged));
        }

        public Task PrepareItemTargetSelectionAsync(byte slotNumber, string selectTargetPlayerMessage)
        {
            if (!HasItemInSlot(slotNumber))
            {
                return Task.CompletedTask;
            }

            setPendingItemSlotNumber(slotNumber);
            setIsTargetSelectionActive(true);
            setLastItemNotification(selectTargetPlayerMessage);
            raiseAllCanExecuteChanged();

            return Task.CompletedTask;
        }

        public async Task OnTargetUserSelectedAsync(
            int userId,
            string unknownErrorMessage,
            string useItemFailureMessagePrefix,
            string useItemUnexpectedErrorMessage,
            string gameWindowTitle)
        {
            if (!getIsTargetSelectionActive())
            {
                return;
            }

            byte? pendingSlot = getPendingItemSlotNumber();
            if (!pendingSlot.HasValue)
            {
                return;
            }

            setIsTargetSelectionActive(false);
            setPendingItemSlotNumber(null);

            await UseItemAsync(
                pendingSlot.Value,
                userId,
                unknownErrorMessage,
                useItemFailureMessagePrefix,
                useItemUnexpectedErrorMessage,
                gameWindowTitle);
        }

        public void CancelItemUse(string itemUseCancelledMessage)
        {
            if (!getIsTargetSelectionActive() && !getPendingItemSlotNumber().HasValue)
            {
                return;
            }

            setPendingItemSlotNumber(null);
            setIsTargetSelectionActive(false);
            setLastItemNotification(itemUseCancelledMessage);
            raiseAllCanExecuteChanged();
        }

        public bool HasItemInSlot(byte slotNumber)
        {
            if (inventory == null)
            {
                return false;
            }

            switch (slotNumber)
            {
                case ITEM_SLOT_1:
                    return inventory.Slot1Item != null && inventory.Slot1Item.Quantity > 0;

                case ITEM_SLOT_2:
                    return inventory.Slot2Item != null && inventory.Slot2Item.Quantity > 0;

                case ITEM_SLOT_3:
                    return inventory.Slot3Item != null && inventory.Slot3Item.Quantity > 0;

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
            if (getIsUseItemInProgress())
            {
                return;
            }

            IGameplayClient gameplayClient = getGameplayClient();
            if (gameplayClient == null)
            {
                return;
            }

            setIsUseItemInProgress(true);
            raiseAllCanExecuteChanged();

            try
            {
                int? targetUserIdOrNull = targetUserId <= 0 ? (int?)null : targetUserId;

                UseItemResponseDto response = await gameplayClient
                    .UseItemAsync(gameId, localUserId, slotNumber, targetUserIdOrNull)
                    .ConfigureAwait(false);

                if (response == null || !response.Success)
                {
                    string failureReason = response != null && !string.IsNullOrWhiteSpace(response.FailureReason)
                        ? response.FailureReason
                        : unknownErrorMessage;

                    logger.Warn("UseItem failed: " + failureReason);

                    await Application.Current.Dispatcher.InvokeAsync(
                        () =>
                        {
                            MessageBox.Show(
                                useItemFailureMessagePrefix + failureReason,
                                gameWindowTitle,
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        });

                    return;
                }

                logger.InfoFormat(
                    "UseItem OK. GameId={0}, UserId={1}, Slot={2}, TargetUserId={3}",
                    gameId,
                    localUserId,
                    slotNumber,
                    targetUserIdOrNull);

                await refreshInventoryAsync().ConfigureAwait(false);
                await syncGameStateAsync().ConfigureAwait(false);
            }
            catch (FaultException faultEx)
            {
                string failureReason = string.IsNullOrWhiteSpace(faultEx.Message)
                    ? unknownErrorMessage
                    : faultEx.Message;

                logger.Warn("UseItem business error: " + failureReason, faultEx);

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        MessageBox.Show(
                            useItemFailureMessagePrefix + failureReason,
                            gameWindowTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
            }
            catch (Exception ex)
            {
                logger.Error(useItemUnexpectedErrorMessage, ex);

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        MessageBox.Show(
                            useItemUnexpectedErrorMessage,
                            gameWindowTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
            }
            finally
            {
                setIsUseItemInProgress(false);
                raiseAllCanExecuteChanged();
            }
        }
    }
}
