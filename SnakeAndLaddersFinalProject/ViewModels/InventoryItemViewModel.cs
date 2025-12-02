using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class InventoryItemViewModel : INotifyPropertyChanged
    {
        private int _quantity;
        private byte? _slotNumber;
        private string _iconPath;



        public event PropertyChangedEventHandler PropertyChanged;

        public int ObjectId { get; set; }

        public string ObjectCode { get; set; }

        public string Name { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity == value)
                {
                    return;
                }

                _quantity = value;
                OnPropertyChanged();
            }
        }

        public byte? SlotNumber
        {
            get => _slotNumber;
            set
            {
                if (_slotNumber == value)
                {
                    return;
                }

                _slotNumber = value;
                OnPropertyChanged();
            }
        }

        public string IconPath
        {
            get => _iconPath;
            set
            {
                if (_iconPath == value)
                {
                    return;
                }

                _iconPath = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
