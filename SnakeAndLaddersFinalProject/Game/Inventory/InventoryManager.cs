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

        private static readonly ILog _logger =
            LogManager.GetLogger(typeof(InventoryManager));

        public async Task<InventorySnapshot> GetInventoryAsync(int userId)
        {
            try
            {
                using (var client = CreateClient())
                {
                    var snapshotDto = await Task
                        .Run(() => client.GetInventory(userId))
                        .ConfigureAwait(false);

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
            }
            catch (Exception ex)
            {
                const string message = "Error loading inventory from service.";
                _logger.Error(message, ex);
                throw new InvalidOperationException(message, ex);
            }
        }

        public async Task UpdateSelectedItemsAsync(int userId, int? slot1ObjectId,
            int? slot2ObjectId, int? slot3ObjectId)
        {
            try
            {
                using (var client = CreateClient())
                {
                    await Task
                        .Run(
                            () => client.UpdateSelectedItems(
                                userId,
                                slot1ObjectId,
                                slot2ObjectId,
                                slot3ObjectId))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                const string message = "Error updating selected items.";
                _logger.Error(message, ex);
                throw new InvalidOperationException(message, ex);
            }
        }

        public async Task UpdateSelectedDiceAsync(int userId, int? slot1DiceId,
            int? slot2DiceId)
        {
            try
            {
                using (var client = CreateClient())
                {
                    await Task
                        .Run(
                            () => client.UpdateSelectedDice(
                                userId,
                                slot1DiceId,
                                slot2DiceId))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                const string message = "Error updating selected dice.";
                _logger.Error(message, ex);
                throw new InvalidOperationException(message, ex);
            }
        }

        public async Task EquipItemToSlotAsync(int userId, byte slotNumber,
            int objectId)
        {
            try
            {
                using (var client = CreateClient())
                {
                    await Task
                        .Run(
                            () => client.EquipItemToSlot(
                                userId,
                                slotNumber,
                                objectId))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                const string message = "Error equipping item to slot.";
                _logger.Error(message, ex);
                throw new InvalidOperationException(message, ex);
            }
        }

        public async Task UnequipItemFromSlotAsync(int userId, byte slotNumber)
        {
            try
            {
                using (var client = CreateClient())
                {
                    await Task
                        .Run(
                            () => client.UnequipItemFromSlot(
                                userId,
                                slotNumber))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                const string message = "Error unequipping item from slot.";
                _logger.Error(message, ex);
                throw new InvalidOperationException(message, ex);
            }
        }

        public async Task EquipDiceToSlotAsync(
            int userId,
            byte slotNumber,
            int diceId)
        {
            try
            {
                using (var client = CreateClient())
                {
                    await Task
                        .Run(
                            () => client.EquipDiceToSlot(
                                userId,
                                slotNumber,
                                diceId))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                const string message = "Error equipping dice to slot.";
                _logger.Error(message, ex);
                throw new InvalidOperationException(message, ex);
            }
        }

        public async Task UnequipDiceFromSlotAsync(int userId, byte slotNumber)
        {
            try
            {
                using (var client = CreateClient())
                {
                    await Task
                        .Run(
                            () => client.UnequipDiceFromSlot(
                                userId,
                                slotNumber))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                const string message = "Error unequipping dice from slot.";
                _logger.Error(message, ex);
                throw new InvalidOperationException(message, ex);
            }
        }

        private static InventoryServiceClient CreateClient()
        {
            return new InventoryServiceClient(INVENTORY_SERVICE_ENDPOINT_NAME);
        }
    }
}
