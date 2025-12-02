using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using log4net;
using SnakeAndLaddersFinalProject.Navigation;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class CreateMatchViewModel : INotifyPropertyChanged
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CreateMatchViewModel));

        private const string STATUS_CREATE_ERROR_PREFIX = "Ocurrió un error al crear la partida: ";

        private BoardSizeOption _boardSize = BoardSizeOption.TenByTen;
        private DifficultyOption _difficulty = DifficultyOption.Medium;
        private SpecialTileOptions _specialTiles = SpecialTileOptions.None;
        private bool _isPrivate;
        private int _players = AppConstants.DEFAULT_PLAYERS;
        private string _errorMessage = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<LobbyNavigationArgs> NavigateToLobbyRequested;

        public BoardSizeOption BoardSize
        {
            get { return _boardSize; }
            set
            {
                if (_boardSize == value)
                {
                    return;
                }

                _boardSize = value;
                OnPropertyChanged();
            }
        }

        public DifficultyOption Difficulty
        {
            get { return _difficulty; }
            set
            {
                if (_difficulty == value)
                {
                    return;
                }

                _difficulty = value;
                OnPropertyChanged();
            }
        }

        public SpecialTileOptions SpecialTiles
        {
            get { return _specialTiles; }
            set
            {
                if (_specialTiles == value)
                {
                    return;
                }

                _specialTiles = value;
                OnPropertyChanged();
            }
        }

        public bool IsPrivate
        {
            get { return _isPrivate; }
            set
            {
                if (_isPrivate == value)
                {
                    return;
                }

                _isPrivate = value;
                OnPropertyChanged();
            }
        }

        public int Players
        {
            get { return _players; }
            set
            {
                if (_players == value)
                {
                    return;
                }

                _players = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set
            {
                if (_errorMessage == value)
                {
                    return;
                }

                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand CreateRoomCommand { get; }

        public CreateMatchViewModel()
        {
            CreateRoomCommand = new RelayCommand(_ => CreateRoom());
        }

        private void CreateRoom()
        {
            try
            {
                var options = new CreateMatchOptions
                {
                    BoardSize = BoardSize,
                    Difficulty = Difficulty,
                    SpecialTiles = SpecialTiles,
                    IsPrivate = IsPrivate,
                    Players = Players
                };

                var args = new LobbyNavigationArgs
                {
                    Mode = LobbyEntryMode.Create,
                    CreateOptions = options
                };

                NavigateToLobbyRequested?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.Error("Error al preparar la navegación al lobby.", ex);
                ErrorMessage = STATUS_CREATE_ERROR_PREFIX + ex.Message;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
