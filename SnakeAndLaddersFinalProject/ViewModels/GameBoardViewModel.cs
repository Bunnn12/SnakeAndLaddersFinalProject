using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardViewModel));

        private const int MIN_INDEX = 1;

        private const int ANIMATION_STEP_MS = 150;
        private const int BOB_STEP_MS = 60;
        private const double BOB_OFFSET = -5.0;

        private bool isAnimatingLocalMove;

        private readonly IGameplayClient gameplayClient;
        private readonly int gameId;
        private readonly int localUserId;
        private readonly DispatcherTimer statePollTimer;

        private int startCellIndex = MIN_INDEX;

        public int Rows { get; }
        public int Columns { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }
        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }

        public CornerPlayersViewModel CornerPlayers { get; }

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

            var startCell = Cells.FirstOrDefault(c => c.IsStart);
            if (startCell != null)
            {
                startCellIndex = startCell.Index;
            }

            RollDiceCommand = new AsyncCommand(RollDiceForLocalPlayerAsync);

            statePollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            statePollTimer.Tick += async (_, __) => await SyncGameStateAsync();
            statePollTimer.Start();
        }

        public void InitializeCornerPlayers(IList<LobbyMemberViewModel> lobbyMembers)
        {
            CornerPlayers.InitializeFromLobbyMembers(lobbyMembers);
        }

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

            startCellIndex = startIndex;

            foreach (var member in lobbyMembers)
            {
                PlayerTokens.Add(
                    new PlayerTokenViewModel(
                        member.UserId,
                        member.UserName,
                        member.CurrentSkinUnlockedId,
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

        // ===================== MAPEO / TOKENS =================================

        private int MapServerIndexToVisual(int serverIndex)
        {
            if (serverIndex == 0)
            {
                return startCellIndex;
            }

            return serverIndex;
        }

        private PlayerTokenViewModel GetOrCreateTokenForUser(int userId, int initialCellIndex)
        {
            var token = PlayerTokens.FirstOrDefault(t => t.UserId == userId);

            if (token != null)
            {
                return token;
            }

            token = new PlayerTokenViewModel(
                userId,
                $"Jugador {userId}",
                null,
                initialCellIndex);

            PlayerTokens.Add(token);
            return token;
        }

        private void ApplyMoveToToken(int userId, int fromIndexServer, int toIndexServer)
        {
            int fromIndexVisual = MapServerIndexToVisual(fromIndexServer);
            int toIndexVisual = MapServerIndexToVisual(toIndexServer);

            var token = PlayerTokens.FirstOrDefault(t => t.UserId == userId);

            if (token == null)
            {
                Logger.WarnFormat(
                    "Token para el usuario {0} no encontrado en el tablero. Tokens actuales: [{1}]",
                    userId,
                    string.Join(", ", PlayerTokens.Select(t => t.UserId)));

                int initialIndex = fromIndexVisual > 0 ? fromIndexVisual : startCellIndex;

                token = new PlayerTokenViewModel(userId, $"Jugador {userId}", null, initialIndex);
                PlayerTokens.Add(token);
            }

            token.CurrentCellIndex = toIndexVisual;
        }

        // ===================== ANIMACIÓN LOCAL =================================

        private async Task AnimateMoveForLocalPlayerAsync(
            int userId,
            int fromIndexServer,
            int toIndexServer)
        {
            int fromVisual = MapServerIndexToVisual(fromIndexServer);
            int toVisual = MapServerIndexToVisual(toIndexServer);

            var token = GetOrCreateTokenForUser(userId, fromVisual);

            if (fromVisual == toVisual)
            {
                token.CurrentCellIndex = toVisual;
                return;
            }

            isAnimatingLocalMove = true;

            try
            {
                if (fromVisual < toVisual)
                {
                    for (int i = fromVisual + 1; i <= toVisual; i++)
                    {
                        token.CurrentCellIndex = i;
                        await BobTokenAsync(token);
                        await Task.Delay(ANIMATION_STEP_MS);
                    }
                }
                else
                {
                    for (int i = fromVisual - 1; i >= toVisual; i--)
                    {
                        token.CurrentCellIndex = i;
                        await BobTokenAsync(token);
                        await Task.Delay(ANIMATION_STEP_MS);
                    }
                }
            }
            finally
            {
                isAnimatingLocalMove = false;
            }
        }

        private async Task BobTokenAsync(PlayerTokenViewModel token)
        {
            if (token == null)
            {
                return;
            }

            token.VerticalOffset = 0;
            await Task.Delay(BOB_STEP_MS);

            token.VerticalOffset = BOB_OFFSET;
            await Task.Delay(BOB_STEP_MS);

            token.VerticalOffset = 0;
            await Task.Delay(BOB_STEP_MS);
        }

        // ===================== ROLL DICE LOCAL =================================

        private async Task RollDiceForLocalPlayerAsync()
        {
            try
            {
                // IMPORTANTE: SIN ConfigureAwait(false) para seguir en el hilo de UI
                var response = await gameplayClient
                    .GetRollDiceAsync(gameId, localUserId);

                if (response == null || !response.Success)
                {
                    string failureReason = response?.FailureReason ?? "Unknown error.";

                    Logger.Warn("RollDice failed: " + failureReason);

                    MessageBox.Show(
                        "No se pudo tirar el dado: " + failureReason,
                        "Juego",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

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

                // ya estamos en el hilo de UI: podemos animar directo
                await AnimateMoveForLocalPlayerAsync(userId, fromIndex, toIndex);

                string message =
                    $"Sacaste {diceValue} y avanzaste de la casilla {fromIndex} a la casilla {toIndex}.";

                MessageBox.Show(
                    message,
                    "Resultado del dado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error("Error inesperado al tirar el dado.", ex);

                MessageBox.Show(
                    "Ocurrió un error inesperado al tirar el dado.",
                    "Juego",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ===================== SINCRONIZACIÓN GLOBAL ==========================

        private async Task SyncGameStateAsync()
        {
            try
            {
                if (isAnimatingLocalMove)
                {
                    return;
                }

                var stateResponse = await gameplayClient
                    .GetGameStateAsync(gameId)
                    .ConfigureAwait(false);

                if (stateResponse == null)
                {
                    return;
                }

                var tokens = stateResponse.Tokens ?? Array.Empty<TokenStateDto>();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var tokenState in tokens)
                    {
                        ApplyMoveToToken(
                            tokenState.UserId,
                            tokenState.CellIndex,
                            tokenState.CellIndex);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Error al sincronizar el estado de la partida.", ex);
            }
        }
    }
}
