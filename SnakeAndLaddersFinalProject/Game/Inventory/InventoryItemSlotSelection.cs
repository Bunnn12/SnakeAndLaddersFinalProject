using SnakeAndLaddersFinalProject.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Game.Inventory
{
    public class InventoryItemSlotSelection
    {
        public byte SlotNumber { get; set; }

        public InventoryItemViewModel Item { get; set; }
    }
}
