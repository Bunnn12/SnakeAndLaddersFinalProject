using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using log4net;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Navigation;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class CreateMatchViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CreateMatchViewModel));

        private const string STATUS_CREATE_ERROR_PREFIX = "Ocurrió un error al crear la partida: ";

        private BoardSizeOption boardSize = BoardSizeOption.TenByTen;
        private DifficultyOption difficulty = DifficultyOption.Medium;
        private SpecialTileOptions specialTiles = SpecialTileOptions.None;
        private bool isPrivate;
        private int players = AppConstants.DEFAULT_PLAYERS;
        private string errorMessage = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<LobbyNavigationArgs> NavigateToLobbyRequested;

        public BoardSizeOption BoardSize
        {
            get { return boardSize; }
            set
            {
                if (boardSize == value)
                {
                    return;
                }

                boardSize = value;
                OnPropertyChanged();
            }
        }

        public DifficultyOption Difficulty
        {
            get { return difficulty; }
            set
            {
                if (difficulty == value)
                {
                    return;
                }

                difficulty = value;
                OnPropertyChanged();
            }
        }

        public SpecialTileOptions SpecialTiles
        {
            get { return specialTiles; }
            set
            {
                if (specialTiles == value)
                {
                    return;
                }

                specialTiles = value;
                OnPropertyChanged();
            }
        }

        public bool IsPrivate
        {
            get { return isPrivate; }
            set
            {
                if (isPrivate == value)
                {
                    return;
                }

                isPrivate = value;
                OnPropertyChanged();
            }
        }

        public int Players
        {
            get { return players; }
            set
            {
                if (players == value)
                {
                    return;
                }

                players = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get { return errorMessage; }
            private set
            {
                if (errorMessage == value)
                {
                    return;
                }

                errorMessage = value;
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
                Logger.Error("Error al preparar la navegación al lobby.", ex);
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
