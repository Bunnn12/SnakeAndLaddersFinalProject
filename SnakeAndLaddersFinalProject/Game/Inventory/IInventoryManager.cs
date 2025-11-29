using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Game.Inventory
{
    public interface IInventoryManager
    {
        Task<InventorySnapshot> GetInventoryAsync(int userId);

        Task UpdateSelectedItemsAsync(
            int userId,
            int? slot1ObjectId,
            int? slot2ObjectId,
            int? slot3ObjectId);

        Task UpdateSelectedDiceAsync(
            int userId,
            int? slot1DiceId,
            int? slot2DiceId);

        Task EquipItemToSlotAsync(
            int userId,
            byte slotNumber,
            int objectId);

        Task UnequipItemFromSlotAsync(
            int userId,
            byte slotNumber);

        Task EquipDiceToSlotAsync(
            int userId,
            byte slotNumber,
            int diceId);

        Task UnequipDiceFromSlotAsync(
            int userId,
            byte slotNumber);
    }

}
