using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using log4net;
using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using SnakeAndLaddersFinalProject.Services;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardViewModel));

        private const int MIN_INDEX = 1;

        private readonly IGameplayClient gameplayClient;
        private readonly int gameId;
        private readonly int localUserId;

        public int Rows { get; }
        public int Columns { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }
        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }

        public CornerPlayersViewModel CornerPlayers { get; }

        /// <summary>
        /// Fichas de jugadores sobre el tablero.
        /// </summary>
        public ObservableCollection<PlayerTokenViewModel> PlayerTokens { get; }

        public ICommand RollDiceCommand { get; }

        public GameBoardViewModel(
            BoardDefinitionDto boardDefinition,
            IGameplayClient gameplayClient,
            int gameId,
            int localUserId)
        {
            if (boardDefinition == null)
            {
                throw new ArgumentNullException(nameof(boardDefinition));
            }

            if (gameplayClient == null)
            {
                throw new ArgumentNullException(nameof(gameplayClient));
            }

            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            if (localUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(localUserId));
            }

            this.gameplayClient = gameplayClient;
            this.gameId = gameId;
            this.localUserId = localUserId;

            Rows = boardDefinition.Rows;
            Columns = boardDefinition.Columns;

            Cells = new ObservableCollection<GameBoardCellViewModel>();
            Connections = new ObservableCollection<GameBoardConnectionViewModel>();
            CornerPlayers = new CornerPlayersViewModel();
            PlayerTokens = new ObservableCollection<PlayerTokenViewModel>();

            BuildCells(boardDefinition.Cells);
            BuildConnections(boardDefinition.Links);

            RollDiceCommand = new AsyncCommand(RollDiceForLocalPlayerAsync);
        }

        public void InitializeCornerPlayers(IList<LobbyMemberViewModel> lobbyMembers)
        {
            CornerPlayers.InitializeFromLobbyMembers(lobbyMembers);
        }

        /// <summary>
        /// Inicializa las fichas de los jugadores en la casilla inicial.
        /// </summary>
        public void InitializeTokensFromLobbyMembers(IList<LobbyMemberViewModel> lobbyMembers)
        {
            PlayerTokens.Clear();

            if (lobbyMembers == null || lobbyMembers.Count == 0)
            {
                return;
            }

            int startIndex = MIN_INDEX;
            var startCell = Cells.FirstOrDefault(c => c.IsStart);
            if (startCell != null)
            {
                startIndex = startCell.Index;
            }

            foreach (var member in lobbyMembers)
            {
                PlayerTokens.Add(
                    new PlayerTokenViewModel(
                        member.UserId,
                        member.UserName,
                        member.CurrentSkinId,
                        startIndex));
            }
        }

        private void BuildCells(IList<BoardCellDto> cellDtos)
        {
            if (cellDtos == null)
            {
                throw new ArgumentNullException(nameof(cellDtos));
            }

            var cellsByIndex = cellDtos.ToDictionary(c => c.Index);

            for (int rowFromTop = 0; rowFromTop < Rows; rowFromTop++)
            {
                int rowFromBottom = (Rows - 1) - rowFromTop;

                for (int columnFromLeft = 0; columnFromLeft < Columns; columnFromLeft++)
                {
                    int zeroBasedIndex = (rowFromBottom * Columns) + columnFromLeft;
                    int index = zeroBasedIndex + MIN_INDEX;

                    if (!cellsByIndex.TryGetValue(index, out var cellDto))
                    {
                        throw new InvalidOperationException(
                            $"No se encontró la celda con índice {index}.");
                    }

                    Cells.Add(new GameBoardCellViewModel(cellDto));
                }
            }
        }

        private void BuildConnections(IList<BoardLinkDto> links)
        {
            if (links == null)
            {
                return;
            }

            foreach (var link in links)
            {
                var connectionViewModel = new GameBoardConnectionViewModel(
                    link,
                    Rows,
                    Columns,
                    Cells);

                Connections.Add(connectionViewModel);
            }
        }

        private async Task RollDiceForLocalPlayerAsync()
        {
            try
            {
                var response = await gameplayClient
                    .RollDiceAsync(gameId, localUserId)
                    .ConfigureAwait(false);

                if (response == null || !response.Success)
                {
                    string failureReason = response?.FailureReason ?? "Unknown error.";

                    Logger.Warn("RollDice failed: " + failureReason);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "No se pudo tirar el dado: " + failureReason,
                            "Juego",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });

                    return;
                }

                // Usamos el usuario local (el método ya tira por el usuario local)
                int userId = localUserId;
                int fromIndex = response.FromCellIndex;
                int toIndex = response.ToCellIndex;
                int diceValue = response.DiceValue;

                Logger.InfoFormat(
                    "RollDice result: UserId={0}, From={1}, To={2}, Dice={3}",
                    userId,
                    fromIndex,
                    toIndex,
                    diceValue);

                var token = PlayerTokens.FirstOrDefault(t => t.UserId == userId);

                if (token == null)
                {
                    Logger.WarnFormat(
                        "Token para el usuario {0} no encontrado en el tablero. Tokens actuales: [{1}]",
                        userId,
                        string.Join(", ", PlayerTokens.Select(t => t.UserId)));

                    // Intento de autocorrección: crear la ficha si no existe
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        int startIndex =
                            fromIndex > 0
                                ? fromIndex
                                : MIN_INDEX;

                        var newToken = new PlayerTokenViewModel(
                            userId,
                            $"Jugador {userId}",
                            null,
                            startIndex);

                        PlayerTokens.Add(newToken);
                        token = newToken;
                    });

                    if (token == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                "No se encontró la ficha del jugador en el tablero.",
                                "Juego",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        });

                        return;
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    token.CurrentCellIndex = toIndex;

                    string message =
                        $"Sacaste {diceValue} y avanzaste de la casilla {fromIndex} a la casilla {toIndex}.";

                    MessageBox.Show(
                        message,
                        "Resultado del dado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Error inesperado al tirar el dado.", ex);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "Ocurrió un error inesperado al tirar el dado.",
                        "Juego",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }


    }
}
