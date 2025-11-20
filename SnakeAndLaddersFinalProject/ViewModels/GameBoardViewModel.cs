using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.Animation;
using SnakeAndLaddersFinalProject.Game;
using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel : INotifyPropertyChanged, IGameplayEventsHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardViewModel));

        private const int MIN_INDEX = 1;
        private const double CELL_CENTER_VERTICAL_ADJUST = -0.18;
        private const int STATE_POLL_INTERVAL_SECONDS = 1;

        private const string DICE_ROLL_SPRITE_PATH = "pack://application:,,,/Assets/Images/Dice/DiceSpriteSheet.png";
        private const string DICE_FACE_BASE_PATH = "pack://application:,,,/Assets/Images/Dice/";

        // ================================================================
        // CAMPOS PRIVADOS
        // ================================================================

        private IGameplayClient gameplayClient;
        private readonly int gameId;
        private readonly int localUserId;

        private readonly DispatcherTimer statePollTimer;

        private readonly Dictionary<int, Point> cellCentersByIndex;
        private readonly Dictionary<int, BoardLinkDto> linksByStartIndex;

        private readonly PlayerTokenManager tokenManager;
        private readonly GameBoardAnimationService animationService;
        private readonly DiceSpriteAnimator diceAnimator;
        private readonly AsyncCommand rollDiceCommand;

        private readonly int startCellIndex = MIN_INDEX;

        private int currentTurnUserId;
        private bool isMyTurn;
        


        // ================================================================
        // PROPIEDADES PUBLICAS
        // ================================================================

        public int Rows { get; }
        public int Columns { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }
        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }
        public CornerPlayersViewModel CornerPlayers { get; }

        public ObservableCollection<PlayerTokenViewModel> PlayerTokens => tokenManager.PlayerTokens;

        public ICommand RollDiceCommand => rollDiceCommand;

        public DiceSpriteAnimator DiceAnimator => diceAnimator;

        public bool IsMyTurn
        {
            get => isMyTurn;
            private set
            {
                if (isMyTurn == value)
                    return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    isMyTurn = value;
                    OnPropertyChanged();
                    rollDiceCommand?.RaiseCanExecuteChanged();
                });
            }
        }

        // ================================================================
        // CONSTRUCTOR
        // ================================================================

        public GameBoardViewModel(
    BoardDefinitionDto boardDefinition,
    int gameId,
    int localUserId,
    string currentUserName)
        {
            ValidateConstructorArguments(boardDefinition, gameId, localUserId, currentUserName);

            this.gameId = gameId;
            this.localUserId = localUserId;

            Rows = boardDefinition.Rows;
            Columns = boardDefinition.Columns;

            Cells = new ObservableCollection<GameBoardCellViewModel>();
            Connections = new ObservableCollection<GameBoardConnectionViewModel>();
            CornerPlayers = new CornerPlayersViewModel();

            cellCentersByIndex = new Dictionary<int, Point>();
            linksByStartIndex = new Dictionary<int, BoardLinkDto>();

            BuildCells(boardDefinition.Cells);
            BuildConnections(boardDefinition.Links);

            var startCell = Cells.FirstOrDefault(c => c.IsStart);
            if (startCell != null)
            {
                startCellIndex = startCell.Index;
            }

            tokenManager = new PlayerTokenManager(
                new ObservableCollection<PlayerTokenViewModel>(),
                cellCentersByIndex);

            animationService = new GameBoardAnimationService(
                tokenManager,
                linksByStartIndex,
                cellCentersByIndex,
                MapServerIndexToVisual);

            diceAnimator = new DiceSpriteAnimator(
                DICE_ROLL_SPRITE_PATH,
                DICE_FACE_BASE_PATH);

            rollDiceCommand = new AsyncCommand(
                RollDiceForLocalPlayerAsync,
                CanRollDice);

            statePollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(STATE_POLL_INTERVAL_SECONDS)
            };
            statePollTimer.Tick += async (_, __) => await SyncGameStateAsync();
        }



        public async Task InitializeGameplayAsync(IGameplayClient client, string currentUserName)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            gameplayClient = client;

            statePollTimer.Start();

            string safeUserName = string.IsNullOrWhiteSpace(currentUserName)
                ? $"User {localUserId}"
                : currentUserName.Trim();

            await JoinGameplayAsync(safeUserName);
            await SyncGameStateAsync();
        }



        // ================================================================
        // VALIDACIONES
        // ================================================================

        private static void ValidateConstructorArguments(
    BoardDefinitionDto boardDefinition,
    int gameId,
    int localUserId,
    string currentUserName)
        {
            if (boardDefinition == null)
            {
                throw new ArgumentNullException(nameof(boardDefinition));
            }

            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            if (localUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(localUserId));
            }

            if (string.IsNullOrWhiteSpace(currentUserName))
            {
                
            }
        }


        // ================================================================
        // INICIALIZACION DE JUGADORES Y TOKENS
        // ================================================================

        public void InitializeCornerPlayers(IList<LobbyMemberViewModel> lobbyMembers)
        {
            CornerPlayers.InitializeFromLobbyMembers(lobbyMembers);
        }

        public void InitializeTokensFromLobbyMembers(IList<LobbyMemberViewModel> lobbyMembers)
        {
            tokenManager.PlayerTokens.Clear();

            if (lobbyMembers == null || lobbyMembers.Count == 0)
                return;

            foreach (var member in lobbyMembers)
                tokenManager.CreateFromLobbyMember(member, startCellIndex);

            tokenManager.ResetAllTokensToCell(startCellIndex);
        }

        private async Task JoinGameplayAsync(string currentUserName)
        {
            try
            {
                await gameplayClient.JoinGameAsync(gameId, localUserId, currentUserName);
                Logger.InfoFormat(
                    "JoinGame OK. GameId={0}, UserId={1}, UserName={2}",
                    gameId,
                    localUserId,
                    currentUserName);
            }
            catch (Exception ex)
            {
                Logger.Error("Error al registrarse para callbacks de gameplay.", ex);
            }
        }


        // ================================================================
        // CONSTRUCCION DEL TABLERO
        // ================================================================

        private void BuildCells(IList<BoardCellDto> cellDtos)
        {
            if (cellDtos == null)
                throw new ArgumentNullException(nameof(cellDtos));

            var cellsByIndex = cellDtos.ToDictionary(c => c.Index);

            for (int rowFromTop = 0; rowFromTop < Rows; rowFromTop++)
            {
                int rowFromBottom = (Rows - 1) - rowFromTop;

                for (int columnFromLeft = 0; columnFromLeft < Columns; columnFromLeft++)
                {
                    int zeroBasedIndex = (rowFromBottom * Columns) + columnFromLeft;
                    int index = zeroBasedIndex + MIN_INDEX;

                    if (!cellsByIndex.TryGetValue(index, out var cellDto))
                        throw new InvalidOperationException($"No se encontró la celda con índice {index}.");

                    Cells.Add(new GameBoardCellViewModel(cellDto));

                    double centerX = columnFromLeft + 0.5;
                    double centerY = rowFromTop + 0.5 + CELL_CENTER_VERTICAL_ADJUST;
                    cellCentersByIndex[index] = new Point(centerX, centerY);
                }
            }
        }

        private void BuildConnections(IList<BoardLinkDto> links)
        {
            if (links == null)
                return;

            foreach (var link in links)
            {
                if (!linksByStartIndex.ContainsKey(link.StartIndex))
                    linksByStartIndex[link.StartIndex] = link;

                var connectionViewModel = new GameBoardConnectionViewModel(
                    link,
                    Rows,
                    Columns,
                    Cells);

                Connections.Add(connectionViewModel);
            }
        }

        private int MapServerIndexToVisual(int serverIndex)
        {
            return serverIndex == 0 ? startCellIndex : serverIndex;
        }

        // ================================================================
        // DADO
        // ================================================================

        private bool CanRollDice()
        {
            Logger.InfoFormat(
                "CanRollDice: isMyTurn={0}, isAnimating={1}, isRolling={2}",
                IsMyTurn,
                animationService.IsAnimating,
                diceAnimator.IsRolling);

            if (!IsMyTurn)
                return false;

            if (animationService.IsAnimating)
                return false;

            if (diceAnimator.IsRolling)
                return false;

            return true;
        }



        private async Task RollDiceForLocalPlayerAsync()
        {
            try
            {
                var response = await gameplayClient
                    .GetRollDiceAsync(gameId, localUserId)
                    .ConfigureAwait(false);

                if (response == null || !response.Success)
                {
                    string failureReason = response?.FailureReason ?? "Unknown error.";

                    Logger.Warn("RollDice failed: " + failureReason);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show(
                            "No se pudo tirar el dado: " + failureReason,
                            "Juego",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });

                    return;
                }

                Logger.InfoFormat(
                    "RollDice request accepted. UserId={0}, From={1}, To={2}, Dice={3}",
                    localUserId,
                    response.FromCellIndex,
                    response.ToCellIndex,
                    response.DiceValue);

                // La animación real llega por callback: HandleServerPlayerMovedAsync
            }
            catch (Exception ex)
            {
                Logger.Error("Error inesperado al tirar el dado.", ex);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        "Ocurrió un error inesperado al tirar el dado.",
                        "Juego",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }

        // ================================================================
        // SINCRONIZACION DE ESTADO
        // ================================================================

        private async Task SyncGameStateAsync()
        {
            try
            {
                if (animationService.IsAnimating)
                    return;

                var stateResponse = await gameplayClient
                    .GetGameStateAsync(gameId)
                    .ConfigureAwait(false);

                if (stateResponse == null)
                    return;

                UpdateTurnFromState(stateResponse.CurrentTurnUserId);

                if (stateResponse.Tokens == null)
                    return;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var tokenState in stateResponse.Tokens)
                    {
                        int userId = tokenState.UserId;
                        int cellIndexVisual = MapServerIndexToVisual(tokenState.CellIndex);

                        var token = tokenManager.GetOrCreateTokenForUser(
                            userId,
                            cellIndexVisual);

                        tokenManager.UpdateTokenPositionFromCell(
                            token,
                            cellIndexVisual);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Error al sincronizar el estado de la partida.", ex);
            }
        }

        private void UpdateTurnFromState(int currentTurnUserIdFromServer)
        {
            currentTurnUserId = currentTurnUserIdFromServer;

            bool isMyTurnNow = (currentTurnUserId == localUserId);

            Logger.InfoFormat(
                "UpdateTurnFromState: gameId={0}, localUserId={1}, currentTurnUserId={2}, isMyTurn={3}",
                gameId,
                localUserId,
                currentTurnUserId,
                isMyTurnNow);

            IsMyTurn = isMyTurnNow;
        }


        // ================================================================
        // EVENTOS DESDE SERVIDOR (CALLBACKS)
        // ================================================================

        public async Task HandleServerPlayerMovedAsync(PlayerMoveResultDto move)
        {
            if (move == null)
                return;

            try
            {
                int userId = move.UserId;
                int fromIndex = move.FromCellIndex;
                int toIndex = move.ToCellIndex;
                int diceValue = move.DiceValue;

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await diceAnimator.RollAsync(diceValue);

                    await animationService.AnimateMoveForLocalPlayerAsync(
                        userId,
                        fromIndex,
                        toIndex,
                        diceValue);

                    // ⬇️ Forzar reevaluación del botón cuando terminó la animación
                    rollDiceCommand?.RaiseCanExecuteChanged();

                    if (userId == localUserId)
                    {
                        MessageBox.Show(
                            $"Sacaste {diceValue} y avanzaste de {fromIndex} a {toIndex}.",
                            "Resultado del dado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Error al procesar movimiento desde el servidor.", ex);
            }
        }


        public Task HandleServerTurnChangedAsync(TurnChangedDto turnInfo)
        {
            if (turnInfo == null)
                return Task.CompletedTask;

            try
            {
                UpdateTurnFromState(turnInfo.CurrentTurnUserId);
            }
            catch (Exception ex)
            {
                Logger.Error("Error al procesar cambio de turno.", ex);
            }

            return Task.CompletedTask;
        }

        public Task HandleServerPlayerLeftAsync(PlayerLeftDto playerLeftInfo)
        {
            if (playerLeftInfo == null)
                return Task.CompletedTask;

            try
            {
                if (playerLeftInfo.UserId != localUserId)
                {
                    string userName = string.IsNullOrWhiteSpace(playerLeftInfo.UserName)
                        ? $"Jugador {playerLeftInfo.UserId}"
                        : playerLeftInfo.UserName;

                    MessageBox.Show(
                        $"{userName} abandonó la partida.",
                        "Jugador salió",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                if (playerLeftInfo.NewCurrentTurnUserId.HasValue)
                    UpdateTurnFromState(playerLeftInfo.NewCurrentTurnUserId.Value);
            }
            catch (Exception ex)
            {
                Logger.Error("Error al procesar PlayerLeft.", ex);
            }

            return Task.CompletedTask;
        }

        // ================================================================
        // NOTIFICACIÓN DE CAMBIOS WPF
        // ================================================================

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
