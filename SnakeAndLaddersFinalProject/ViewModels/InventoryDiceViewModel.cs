using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class InventoryDiceViewModel : INotifyPropertyChanged
    {
        private int _quantity;
        private byte? _slotNumber;
        private string _iconPath;

        public event PropertyChangedEventHandler PropertyChanged;

        public int DiceId { get; set; }

        public string DiceCode { get; set; }

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
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
