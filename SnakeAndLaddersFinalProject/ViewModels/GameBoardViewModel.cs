using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.Animation;
using SnakeAndLaddersFinalProject.Game;
using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardViewModel));

        private const int MIN_INDEX = 1;
        private const double CELL_CENTER_VERTICAL_ADJUST = -0.18;
        private const int STATE_POLL_INTERVAL_SECONDS = 1;

        private const string DICE_ROLL_SPRITE_PATH = "pack://application:,,,/Assets/Images/Dice/DiceSpriteSheet.png";
        private const string DICE_FACE_BASE_PATH = "pack://application:,,,/Assets/Images/Dice/";

        private readonly IGameplayClient gameplayClient;
        private readonly int gameId;
        private readonly int localUserId;
        private readonly DispatcherTimer statePollTimer;

        private readonly Dictionary<int, Point> cellCentersByIndex;
        private readonly Dictionary<int, BoardLinkDto> linksByStartIndex;

        private readonly PlayerTokenManager tokenManager;
        private readonly GameBoardAnimationService animationService;
        private readonly DiceSpriteAnimator diceAnimator;

        private readonly int startCellIndex = MIN_INDEX;

        public int Rows { get; }
        public int Columns { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }
        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }
        public CornerPlayersViewModel CornerPlayers { get; }

        public ObservableCollection<PlayerTokenViewModel> PlayerTokens
        {
            get { return tokenManager.PlayerTokens; }
        }

        public ICommand RollDiceCommand { get; }

        public DiceSpriteAnimator DiceAnimator
        {
            get { return diceAnimator; }
        }

        public GameBoardViewModel(
            BoardDefinitionDto boardDefinition,
            IGameplayClient gameplayClient,
            int gameId,
            int localUserId)
        {
            ValidateConstructorArguments(boardDefinition, gameplayClient, gameId, localUserId);

            this.gameplayClient = gameplayClient;
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

            RollDiceCommand = new AsyncCommand(RollDiceForLocalPlayerAsync);

            statePollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(STATE_POLL_INTERVAL_SECONDS)
            };
            statePollTimer.Tick += async (_, __) => await SyncGameStateAsync();
            statePollTimer.Start();
        }

        private static void ValidateConstructorArguments(
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
        }

        public void InitializeCornerPlayers(
            IList<LobbyMemberViewModel> lobbyMembers)
        {
            CornerPlayers.InitializeFromLobbyMembers(lobbyMembers);
        }

        public void InitializeTokensFromLobbyMembers(
            IList<LobbyMemberViewModel> lobbyMembers)
        {
            tokenManager.PlayerTokens.Clear();

            if (lobbyMembers == null || lobbyMembers.Count == 0)
            {
                return;
            }

            foreach (var member in lobbyMembers)
            {
                tokenManager.CreateFromLobbyMember(member, startCellIndex);
            }

            tokenManager.ResetAllTokensToCell(startCellIndex);
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

                    double centerX = columnFromLeft + 0.5;
                    double centerY = rowFromTop + 0.5 + CELL_CENTER_VERTICAL_ADJUST;
                    cellCentersByIndex[index] = new Point(centerX, centerY);
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
                if (!linksByStartIndex.ContainsKey(link.StartIndex))
                {
                    linksByStartIndex[link.StartIndex] = link;
                }

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
            return serverIndex == 0
                ? startCellIndex
                : serverIndex;
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

                await Application.Current.Dispatcher.InvokeAsync(
                    async () =>
                    {
                        await diceAnimator.RollAsync(diceValue);

                        await animationService.AnimateMoveForLocalPlayerAsync(
                            userId,
                            fromIndex,
                            toIndex,
                            diceValue);

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

        private async Task SyncGameStateAsync()
        {
            try
            {
                if (animationService.IsAnimating)
                {
                    return;
                }

                var stateResponse = await gameplayClient
                    .GetGameStateAsync(gameId)
                    .ConfigureAwait(false);

                if (stateResponse == null || stateResponse.Tokens == null)
                {
                    return;
                }

                var tokens = stateResponse.Tokens;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var tokenState in tokens)
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
    }
}
