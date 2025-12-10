using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class AvatarSkinItemViewModel : INotifyPropertyChanged
    {
        private string _avatarCode = string.Empty;
        private string _displayName = string.Empty;
        private bool _isUnlocked;
        private bool _isCurrent;
        private string _imagePath = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public string AvatarCode
        {
            get => _avatarCode;
            set
            {
                if (_avatarCode == value)
                {
                    return;
                }

                _avatarCode = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName == value)
                {
                    return;
                }

                _displayName = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public bool IsUnlocked
        {
            get => _isUnlocked;
            set
            {
                if (_isUnlocked == value)
                {
                    return;
                }

                _isUnlocked = value;
                OnPropertyChanged();
            }
        }

        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                if (_isCurrent == value)
                {
                    return;
                }

                _isCurrent = value;
                OnPropertyChanged();
            }
        }

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath == value)
                {
                    return;
                }

                _imagePath = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
