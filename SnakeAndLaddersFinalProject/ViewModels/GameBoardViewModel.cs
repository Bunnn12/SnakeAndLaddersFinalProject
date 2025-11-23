using log4net;
using SnakeAndLaddersFinalProject.Animation;
using SnakeAndLaddersFinalProject.Game;
using SnakeAndLaddersFinalProject.Game.Board;
using SnakeAndLaddersFinalProject.Game.Gameplay;
using SnakeAndLaddersFinalProject.Game.State;
using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel : INotifyPropertyChanged, IGameplayEventsHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardViewModel));

        private const int STATE_POLL_INTERVAL_SECONDS = 1;

        private const string UNKNOWN_ERROR_MESSAGE = "Unknown error.";
        private const string GAME_WINDOW_TITLE = "Juego";
        private const string ROLL_DICE_FAILURE_MESSAGE_PREFIX = "No se pudo tirar el dado: ";
        private const string ROLL_DICE_UNEXPECTED_ERROR_MESSAGE = "Ocurrió un error inesperado al tirar el dado.";
        private const string GAMEPLAY_CALLBACK_ERROR_LOG_MESSAGE = "Error al registrarse para callbacks de gameplay.";
        private const string GAME_STATE_SYNC_ERROR_LOG_MESSAGE = "Error al sincronizar el estado de la partida.";

        private const string DICE_ROLL_SPRITE_PATH = "pack://application:,,,/Assets/Images/Dice/DiceSpriteSheet.png";
        private const string DICE_FACE_BASE_PATH = "pack://application:,,,/Assets/Images/Dice/";

        private readonly int _gameId;
        private readonly int _localUserId;

        private readonly Dictionary<int, Point> _cellCentersByIndex;
        private readonly Dictionary<int, BoardLinkDto> _linksByStartIndex;

        private readonly PlayerTokenManager _tokenManager;
        private readonly GameBoardAnimationService _animationService;
        private readonly DiceSpriteAnimator _diceAnimator;
        private readonly AsyncCommand _rollDiceCommand;
        private readonly GameBoardStatePoller _statePoller;
        private readonly GameplayEventsHandler _eventsHandler;

        private IGameplayClient _gameplayClient;

        private int _startCellIndex;
        private int _currentTurnUserId;
        private bool _isMyTurn;
        private bool _isRollRequestInProgress;

        public int Rows { get; }
        public int Columns { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }
        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }
        public CornerPlayersViewModel CornerPlayers { get; }

        public ObservableCollection<PlayerTokenViewModel> PlayerTokens
        {
            get { return _tokenManager.PlayerTokens; }
        }

        public ICommand RollDiceCommand
        {
            get { return _rollDiceCommand; }
        }

        public DiceSpriteAnimator DiceAnimator
        {
            get { return _diceAnimator; }
        }

        public bool IsMyTurn
        {
            get { return _isMyTurn; }
            private set
            {
                if (_isMyTurn == value)
                {
                    return;
                }

                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        _isMyTurn = value;
                        OnPropertyChanged();
                        _rollDiceCommand.RaiseCanExecuteChanged();
                    });
            }
        }

        public GameBoardViewModel(
            BoardDefinitionDto boardDefinition,
            int gameId,
            int localUserId,
            string currentUserName)
        {
            ValidateConstructorArguments(boardDefinition, gameId, localUserId);

            _gameId = gameId;
            _localUserId = localUserId;

            Rows = boardDefinition.Rows;
            Columns = boardDefinition.Columns;

            BoardBuildResult boardBuildResult = BoardBuilder.Build(boardDefinition);

            Cells = boardBuildResult.Cells;
            Connections = boardBuildResult.Connections;
            _cellCentersByIndex = boardBuildResult.CellCentersByIndex;
            _linksByStartIndex = boardBuildResult.LinksByStartIndex;
            _startCellIndex = boardBuildResult.StartCellIndex;

            CornerPlayers = new CornerPlayersViewModel();

            ObservableCollection<PlayerTokenViewModel> playerTokens = new ObservableCollection<PlayerTokenViewModel>();
            _tokenManager = new PlayerTokenManager(
                playerTokens,
                _cellCentersByIndex);

            _animationService = new GameBoardAnimationService(
                _tokenManager,
                _linksByStartIndex,
                _cellCentersByIndex,
                MapServerIndexToVisual);

            _diceAnimator = new DiceSpriteAnimator(
                DICE_ROLL_SPRITE_PATH,
                DICE_FACE_BASE_PATH);

            _rollDiceCommand = new AsyncCommand(
                RollDiceForLocalPlayerAsync,
                CanRollDice);

            _eventsHandler = new GameplayEventsHandler(
                _animationService,
                _diceAnimator,
                _rollDiceCommand,
                Logger,
                _localUserId,
                UpdateTurnFromState);

            TimeSpan pollInterval = TimeSpan.FromSeconds(STATE_POLL_INTERVAL_SECONDS);
            _statePoller = new GameBoardStatePoller(
                pollInterval,
                SyncGameStateAsync,
                Logger);
        }

        public async Task InitializeGameplayAsync(IGameplayClient client, string currentUserName)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _gameplayClient = client;

            _statePoller.Start();

            string safeUserName = string.IsNullOrWhiteSpace(currentUserName)
                ? string.Format("User {0}", _localUserId)
                : currentUserName.Trim();

            await JoinGameplayAsync(safeUserName).ConfigureAwait(false);
            await SyncGameStateAsync().ConfigureAwait(false);
        }

        private static void ValidateConstructorArguments(
            BoardDefinitionDto boardDefinition,
            int gameId,
            int localUserId)
        {
            if (boardDefinition == null)
            {
                throw new ArgumentNullException(nameof(boardDefinition));
            }

            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            if (localUserId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(localUserId));
            }
        }

        public void InitializeCornerPlayers(IList<LobbyMemberViewModel> lobbyMembers)
        {
            CornerPlayers.InitializeFromLobbyMembers(lobbyMembers);
        }

        public void InitializeTokensFromLobbyMembers(IList<LobbyMemberViewModel> lobbyMembers)
        {
            _tokenManager.PlayerTokens.Clear();

            if (lobbyMembers == null || lobbyMembers.Count == 0)
            {
                return;
            }

            foreach (LobbyMemberViewModel lobbyMember in lobbyMembers)
            {
                _tokenManager.CreateFromLobbyMember(lobbyMember, _startCellIndex);
            }

            _tokenManager.ResetAllTokensToCell(_startCellIndex);
        }

        private async Task JoinGameplayAsync(string currentUserName)
        {
            try
            {
                await _gameplayClient
                    .JoinGameAsync(_gameId, _localUserId, currentUserName)
                    .ConfigureAwait(false);

                Logger.InfoFormat(
                    "JoinGame OK. GameId={0}, UserId={1}, UserName={2}",
                    _gameId,
                    _localUserId,
                    currentUserName);
            }
            catch (Exception ex)
            {
                Logger.Error(GAMEPLAY_CALLBACK_ERROR_LOG_MESSAGE, ex);
            }
        }

        private int MapServerIndexToVisual(int serverIndex)
        {
            if (serverIndex == 0)
            {
                return _startCellIndex;
            }

            return serverIndex;
        }

        private bool CanRollDice()
        {
            Logger.InfoFormat(
                "CanRollDice: gameId={0}, localUserId={1}, currentTurnUserId={2}, isMyTurn={3}, isAnimating={4}, isRollRequestInProgress={5}",
                _gameId,
                _localUserId,
                _currentTurnUserId,
                IsMyTurn,
                _animationService.IsAnimating,
                _isRollRequestInProgress);

            if (!IsMyTurn)
            {
                return false;
            }

            if (_animationService.IsAnimating)
            {
                return false;
            }

            if (_isRollRequestInProgress)
            {
                return false;
            }

            return true;
        }

        private async Task RollDiceForLocalPlayerAsync()
        {
            if (_isRollRequestInProgress)
            {
                return;
            }

            _isRollRequestInProgress = true;
            _rollDiceCommand.RaiseCanExecuteChanged();

            try
            {
                var response = await _gameplayClient
                    .GetRollDiceAsync(_gameId, _localUserId)
                    .ConfigureAwait(false);

                if (response == null || !response.Success)
                {
                    string failureReason = response != null && !string.IsNullOrWhiteSpace(response.FailureReason)
                        ? response.FailureReason
                        : UNKNOWN_ERROR_MESSAGE;

                    Logger.Warn("RollDice failed: " + failureReason);

                    await Application.Current.Dispatcher.InvokeAsync(
                        () =>
                        {
                            MessageBox.Show(
                                ROLL_DICE_FAILURE_MESSAGE_PREFIX + failureReason,
                                GAME_WINDOW_TITLE,
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        });

                    return;
                }

                Logger.InfoFormat(
                    "RollDice request accepted. UserId={0}, From={1}, To={2}, Dice={3}",
                    _localUserId,
                    response.FromCellIndex,
                    response.ToCellIndex,
                    response.DiceValue);
            }
            catch (Exception ex)
            {
                Logger.Error(ROLL_DICE_UNEXPECTED_ERROR_MESSAGE, ex);

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        MessageBox.Show(
                            ROLL_DICE_UNEXPECTED_ERROR_MESSAGE,
                            GAME_WINDOW_TITLE,
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        _isRollRequestInProgress = false;
                        _rollDiceCommand.RaiseCanExecuteChanged();
                    });
            }
        }

        private async Task SyncGameStateAsync()
        {
            try
            {
                var stateResponse = await _gameplayClient
                    .GetGameStateAsync(_gameId)
                    .ConfigureAwait(false);

                if (stateResponse == null)
                {
                    return;
                }

                // Siempre actualizamos de quién es el turno
                UpdateTurnFromState(stateResponse.CurrentTurnUserId);

                if (stateResponse.Tokens == null)
                {
                    return;
                }

                if (PlayerTokens.Count > 0)
                {
                    return;
                }

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        foreach (var tokenState in stateResponse.Tokens)
                        {
                            int userId = tokenState.UserId;
                            int cellIndexVisual = MapServerIndexToVisual(tokenState.CellIndex);

                            PlayerTokenViewModel playerToken = _tokenManager.GetOrCreateTokenForUser(
                                userId,
                                cellIndexVisual);

                            _tokenManager.UpdateTokenPositionFromCell(
                                playerToken,
                                cellIndexVisual);
                        }
                    });
            }
            catch (Exception ex)
            {
                Logger.Error(GAME_STATE_SYNC_ERROR_LOG_MESSAGE, ex);
            }
        }


        private void UpdateTurnFromState(int currentTurnUserIdFromServer)
        {
            _currentTurnUserId = currentTurnUserIdFromServer;

            bool isMyTurnNow = _currentTurnUserId == _localUserId;

            Logger.InfoFormat(
                "UpdateTurnFromState: gameId={0}, localUserId={1}, currentTurnUserId={2}, isMyTurn={3}",
                _gameId,
                _localUserId,
                _currentTurnUserId,
                isMyTurnNow);

            IsMyTurn = isMyTurnNow;
        }

        public Task HandleServerPlayerMovedAsync(PlayerMoveResultDto move)
        {
            Task handlerTask = _eventsHandler.HandleServerPlayerMovedAsync(move);
            return handlerTask;
        }

        public Task HandleServerTurnChangedAsync(TurnChangedDto turnInfo)
        {
            Task handlerTask = _eventsHandler.HandleServerTurnChangedAsync(turnInfo);
            return handlerTask;
        }

        public Task HandleServerPlayerLeftAsync(PlayerLeftDto playerLeftInfo)
        {
            Task handlerTask = _eventsHandler.HandleServerPlayerLeftAsync(playerLeftInfo);
            return handlerTask;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler == null)
            {
                return;
            }

            PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
            handler(this, args);
        }
    }
}
