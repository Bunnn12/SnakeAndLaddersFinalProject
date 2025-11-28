using System;

namespace SnakeAndLaddersFinalProject.Game.Inventory
{
    public sealed class InventoryDiceData
    {
        public InventoryDiceData(
            int diceId,
            string diceCode,
            string name,
            int quantity,
            byte? slotNumber)
        {
            if (string.IsNullOrWhiteSpace(diceCode))
            {
                throw new ArgumentException("Dice code cannot be null or whitespace.", nameof(diceCode));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
            }

            if (quantity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            DiceId = diceId;
            DiceCode = diceCode;
            Name = name;
            Quantity = quantity;
            SlotNumber = slotNumber;
        }

        public int DiceId { get; }

        public string DiceCode { get; }

        public string Name { get; }

        public int Quantity { get; }

        public byte? SlotNumber { get; }
    }
}
