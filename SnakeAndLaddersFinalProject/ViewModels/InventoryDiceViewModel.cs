using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class InventoryDiceViewModel : INotifyPropertyChanged
    {
        private int quantity;
        private byte? slotNumber;
        private string iconPath;

        public event PropertyChangedEventHandler PropertyChanged;

        public int DiceId { get; set; }

        public string DiceCode { get; set; }

        public string Name { get; set; }

        public int Quantity
        {
            get => quantity;
            set
            {
                if (quantity == value)
                {
                    return;
                }

                quantity = value;
                OnPropertyChanged();
            }
        }

        public byte? SlotNumber
        {
            get => slotNumber;
            set
            {
                if (slotNumber == value)
                {
                    return;
                }

                slotNumber = value;
                OnPropertyChanged();
            }
        }

        public string IconPath
        {
            get => iconPath;
            set
            {
                if (iconPath == value)
                {
                    return;
                }

                iconPath = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
