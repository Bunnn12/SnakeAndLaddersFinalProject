using System;
using System.Collections.Generic;

namespace SnakeAndLaddersFinalProject.Game.Inventory
{
    public sealed class InventorySnapshot
    {

        public IReadOnlyCollection<InventoryItemData> Items { get; }

        public IReadOnlyCollection<InventoryDiceData> Dice { get; }
        public InventorySnapshot(
            IReadOnlyCollection<InventoryItemData> items,
            IReadOnlyCollection<InventoryDiceData> dice)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Dice = dice ?? throw new ArgumentNullException(nameof(dice));
        }

    }
}
