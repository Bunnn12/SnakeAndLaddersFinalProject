using SnakeAndLaddersFinalProject.ViewModels;


namespace SnakeAndLaddersFinalProject.Game.Inventory
{
    public class InventoryItemSlotSelection
    {
        public byte SlotNumber { get; set; }

        public InventoryItemViewModel Item { get; set; }
    }
}
