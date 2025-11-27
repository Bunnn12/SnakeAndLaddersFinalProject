using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class InventoryItemViewModel : INotifyPropertyChanged
    {
        private int quantity;
        private byte? slotNumber;
        private string iconPath;



        public event PropertyChangedEventHandler PropertyChanged;

        public int ObjectId { get; set; }

        public string ObjectCode { get; set; }

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
