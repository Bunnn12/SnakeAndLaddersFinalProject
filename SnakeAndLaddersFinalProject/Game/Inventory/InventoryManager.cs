using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;
using SnakeAndLaddersFinalProject.InventoryService;

namespace SnakeAndLaddersFinalProject.Game.Inventory
{
    public sealed class InventoryManager : IInventoryManager
    {
        private const string INVENTORY_SERVICE_ENDPOINT_NAME =
            "NetTcpBinding_IInventoryService";

        private const string ERROR_LOAD_INVENTORY_MESSAGE =
            "Error loading inventory from service.";

        private const string ERROR_UPDATE_SELECTED_ITEMS_MESSAGE =
            "Error updating selected items.";

        private const string ERROR_UPDATE_SELECTED_DICE_MESSAGE =
            "Error updating selected dice.";

        private const string ERROR_EQUIP_ITEM_MESSAGE =
            "Error equipping item to slot.";

        private const string ERROR_UNEQUIP_ITEM_MESSAGE =
            "Error unequipping item from slot.";

        private const string ERROR_EQUIP_DICE_MESSAGE =
            "Error equipping dice to slot.";

        private const string ERROR_UNEQUIP_DICE_MESSAGE =
            "Error unequipping dice from slot.";

        private const string ERROR_INVALID_USER_ID_MESSAGE =
            "UserId must be greater than zero.";

        private const int INVALID_USER_ID = 0;

        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(InventoryManager));

        public async Task<InventorySnapshot> GetInventoryAsync(int userId)
        {
            ValidateUserId(userId);

            var snapshotDto = await ExecuteServiceCallAsync(
                client => client.GetInventory(userId),
                ERROR_LOAD_INVENTORY_MESSAGE).ConfigureAwait(false);

            var items = new List<InventoryItemData>();
            var dice = new List<InventoryDiceData>();

            if (snapshotDto?.Items != null)
            {
                foreach (var dto in snapshotDto.Items)
                {
                    items.Add(
                        new InventoryItemData(
                            dto.ObjectId,
                            dto.ObjectCode,
                            dto.Name,
                            dto.Quantity,
                            dto.SlotNumber));
                }
            }

            if (snapshotDto?.Dice != null)
            {
                foreach (var dto in snapshotDto.Dice)
                {
                    dice.Add(
                        new InventoryDiceData(
                            dto.DiceId,
                            dto.DiceCode,
                            dto.Name,
                            dto.Quantity,
                            dto.SlotNumber));
                }
            }

            return new InventorySnapshot(items, dice);
        }

        public Task UpdateSelectedItemsAsync(
            int userId,
            int? slot1ObjectId,
            int? slot2ObjectId,
            int? slot3ObjectId)
        {
            ValidateUserId(userId);

            return ExecuteServiceActionAsync(
                client => client.UpdateSelectedItems(
                    userId,
                    slot1ObjectId,
                    slot2ObjectId,
                    slot3ObjectId),
                ERROR_UPDATE_SELECTED_ITEMS_MESSAGE);
        }

        public Task UpdateSelectedDiceAsync(
            int userId,
            int? slot1DiceId,
            int? slot2DiceId)
        {
            ValidateUserId(userId);

            return ExecuteServiceActionAsync(
                client => client.UpdateSelectedDice(
                    userId,
                    slot1DiceId,
                    slot2DiceId),
                ERROR_UPDATE_SELECTED_DICE_MESSAGE);
        }

        public Task EquipItemToSlotAsync(
            int userId,
            byte slotNumber,
            int objectId)
        {
            ValidateUserId(userId);

            return ExecuteServiceActionAsync(
                client => client.EquipItemToSlot(
                    userId,
                    slotNumber,
                    objectId),
                ERROR_EQUIP_ITEM_MESSAGE);
        }

        public Task UnequipItemFromSlotAsync(
            int userId,
            byte slotNumber)
        {
            ValidateUserId(userId);

            return ExecuteServiceActionAsync(
                client => client.UnequipItemFromSlot(
                    userId,
                    slotNumber),
                ERROR_UNEQUIP_ITEM_MESSAGE);
        }

        public Task EquipDiceToSlotAsync(
            int userId,
            byte slotNumber,
            int diceId)
        {
            ValidateUserId(userId);

            return ExecuteServiceActionAsync(
                client => client.EquipDiceToSlot(
                    userId,
                    slotNumber,
                    diceId),
                ERROR_EQUIP_DICE_MESSAGE);
        }

        public Task UnequipDiceFromSlotAsync(
            int userId,
            byte slotNumber)
        {
            ValidateUserId(userId);

            return ExecuteServiceActionAsync(
                client => client.UnequipDiceFromSlot(
                    userId,
                    slotNumber),
                ERROR_UNEQUIP_DICE_MESSAGE);
        }
        private static async Task<T> ExecuteServiceCallAsync<T>(
            Func<InventoryServiceClient, T> serviceCall,
            string errorMessage)
        {
            try
            {
                using (InventoryServiceClient client = CreateClient())
                {
                    return await Task
                        .Run(() => serviceCall(client))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(errorMessage, ex);
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        private static async Task ExecuteServiceActionAsync(
            Action<InventoryServiceClient> serviceAction,
            string errorMessage)
        {
            try
            {
                using (InventoryServiceClient client = CreateClient())
                {
                    await Task
                        .Run(() => serviceAction(client))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(errorMessage, ex);
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        private static InventoryServiceClient CreateClient()
        {
            return new InventoryServiceClient(INVENTORY_SERVICE_ENDPOINT_NAME);
        }

        private static void ValidateUserId(int userId)
        {
            if (userId <= INVALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(userId),
                    ERROR_INVALID_USER_ID_MESSAGE);
            }
        }
    }
}
