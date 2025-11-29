using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;
using SnakeAndLaddersFinalProject.InventoryService;

namespace SnakeAndLaddersFinalProject.Game.Inventory
{
    public sealed class InventoryManager : IInventoryManager
    {
        private const string INVENTORY_SERVICE_ENDPOINT_NAME = "NetTcpBinding_IInventoryService";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(InventoryManager));

        public async Task<InventorySnapshot> GetInventoryAsync(int userId)
        {
            try
            {
                using (InventoryServiceClient client = CreateClient())
                {
                    var snapshotDto = await Task.Run(() => client.GetInventory(userId));

                    List<InventoryItemData> items = new List<InventoryItemData>();
                    List<InventoryDiceData> dice = new List<InventoryDiceData>();

                    if (snapshotDto != null && snapshotDto.Items != null)
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

                    if (snapshotDto != null && snapshotDto.Dice != null)
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
                Logger.Error("Error loading inventory from service.", ex);
                throw;
            }
        }

        public async Task UpdateSelectedItemsAsync(
            int userId,
            int? slot1ObjectId,
            int? slot2ObjectId,
            int? slot3ObjectId)
        {
            try
            {
                using (InventoryServiceClient client = CreateClient())
                {
                    await Task.Run(
                        () => client.UpdateSelectedItems(
                            userId,
                            slot1ObjectId,
                            slot2ObjectId,
                            slot3ObjectId));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating selected items.", ex);
                throw;
            }
        }

        public async Task UpdateSelectedDiceAsync(
            int userId,
            int? slot1DiceId,
            int? slot2DiceId)
        {
            try
            {
                using (InventoryServiceClient client = CreateClient())
                {
                    await Task.Run(
                        () => client.UpdateSelectedDice(
                            userId,
                            slot1DiceId,
                            slot2DiceId));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating selected dice.", ex);
                throw;
            }
        }

        // ===============================================================
        // NUEVOS MÉTODOS: EQUIP / UNEQUIP ITEMS
        // ===============================================================

        public async Task EquipItemToSlotAsync(
            int userId,
            byte slotNumber,
            int objectId)
        {
            try
            {
                using (InventoryServiceClient client = CreateClient())
                {
                    await Task.Run(
                        () => client.EquipItemToSlot(
                            userId,
                            slotNumber,
                            objectId));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error equipping item to slot.", ex);
                throw;
            }
        }

        public async Task UnequipItemFromSlotAsync(
            int userId,
            byte slotNumber)
        {
            try
            {
                using (InventoryServiceClient client = CreateClient())
                {
                    await Task.Run(
                        () => client.UnequipItemFromSlot(
                            userId,
                            slotNumber));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error unequipping item from slot.", ex);
                throw;
            }
        }

        // ===============================================================
        // NUEVOS MÉTODOS: EQUIP / UNEQUIP DICE
        // ===============================================================

        public async Task EquipDiceToSlotAsync(
            int userId,
            byte slotNumber,
            int diceId)
        {
            try
            {
                using (InventoryServiceClient client = CreateClient())
                {
                    await Task.Run(
                        () => client.EquipDiceToSlot(
                            userId,
                            slotNumber,
                            diceId));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error equipping dice to slot.", ex);
                throw;
            }
        }

        public async Task UnequipDiceFromSlotAsync(
            int userId,
            byte slotNumber)
        {
            try
            {
                using (InventoryServiceClient client = CreateClient())
                {
                    await Task.Run(
                        () => client.UnequipDiceFromSlot(
                            userId,
                            slotNumber));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error unequipping dice from slot.", ex);
                throw;
            }
        }

        // ===============================================================

        private static InventoryServiceClient CreateClient()
        {
            return new InventoryServiceClient(INVENTORY_SERVICE_ENDPOINT_NAME);
        }
    }
}
