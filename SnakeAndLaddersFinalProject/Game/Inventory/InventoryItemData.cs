using System;

namespace SnakeAndLaddersFinalProject.Game.Inventory
{
    public sealed class InventoryItemData
    {
        public InventoryItemData(
            int objectId,
            string objectCode,
            string name,
            int quantity,
            byte? slotNumber)
        {
            if (string.IsNullOrWhiteSpace(objectCode))
            {
                throw new ArgumentException("Object code cannot be null or whitespace.", nameof(objectCode));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
            }

            if (quantity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            ObjectId = objectId;
            ObjectCode = objectCode;
            Name = name;
            Quantity = quantity;
            SlotNumber = slotNumber;
        }

        public int ObjectId { get; }

        public string ObjectCode { get; }

        public string Name { get; }

        public int Quantity { get; }

        public byte? SlotNumber { get; }
    }
}
