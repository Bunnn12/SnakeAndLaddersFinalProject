using log4net;
using SnakeAndLaddersFinalProject.Animation;
using SnakeAndLaddersFinalProject.Game;
using SnakeAndLaddersFinalProject.Game.Board;
using SnakeAndLaddersFinalProject.Game.Gameplay;
using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Managers;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels.Models;
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

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel : INotifyPropertyChanged, IGameplayEventsHandler, IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GameBoardViewModel));

        private const byte MIN_DICE_SLOT = 1;
        private const byte MAX_DICE_SLOT = 2;

        private const byte ITEM_SLOT_1 = 1;
        private const byte ITEM_SLOT_2 = 2;
        private const byte ITEM_SLOT_3 = 3;

        private const string DEFAULT_TURN_TIMER_TEXT = "00:30";

        private const string DICE_IMAGE_BASE_RELATIVE_PATH = "Assets/Images/Dice/";
        private const string DICE_ROLL_SPRITE_RELATIVE_PATH = "DiceSpriteSheet.png";

        private const int SERVER_INACTIVITY_TIMEOUT_SECONDS = 45;
        private const int SERVER_INACTIVITY_CHECK_INTERVAL_SECONDS = 5;

        private readonly int _gameId;
        private readonly int _localUserId;

        private readonly PlayerTokenManager _tokenManager;
        private readonly GameBoardAnimationService _animationService;
        private readonly DiceSpriteAnimator _diceAnimator;

        private readonly AsyncCommand _rollDiceCommand;
        private readonly AsyncCommand _useItemFromSlot1Command;
        private readonly AsyncCommand _useItemFromSlot2Command;
        private readonly AsyncCommand _useItemFromSlot3Command;

        private readonly RelayCommand<int> _selectTargetUserCommand;
        private readonly RelayCommand<int> _cancelItemUseCommand;

        private readonly RelayCommand<int> _selectDiceSlot1Command;
        private readonly RelayCommand<int> _selectDiceSlot2Command;

        private readonly int _startCellIndex;

        private readonly ItemUsageManager _itemUsageController;
        private readonly PodiumBuilder _podiumBuilder;
        private readonly DiceSelectionManager _diceSelectionManager;
        private readonly ServerInactivityGuard _serverInactivityGuard;
        private readonly GameStateSynchronizer _gameStateSynchronizer;
        private readonly DiceRollManager _diceRollManager;
        private readonly GameplayServerEventsRouter _serverEventsRouter;

        private readonly Dictionary<int, string> _userNamesById =
            new Dictionary<int, string>();

        private readonly List<LobbyMemberViewModel> _lobbyMembers =
            new List<LobbyMemberViewModel>();

        private IGameplayClient _gameplayClient;

        private bool _isMyTurn;

        private string _turnTimerText = DEFAULT_TURN_TIMER_TEXT;

        private bool _isRollRequestInProgress;

        private bool _isUseItemInProgress;
        private bool _isTargetSelectionActive;
        private byte? _pendingItemSlotNumber;

        private byte? _selectedDiceSlotNumber;
        private bool _isDiceSlot1Selected;
        private bool _isDiceSlot2Selected;

        private string _lastItemNotification;

        private bool _hasGameFinished;

        public event Action<PodiumViewModel> PodiumRequested;
        public event PropertyChangedEventHandler PropertyChanged;

        public int Rows { get; }

        public int Columns { get; }

        public InventoryViewModel Inventory { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }

        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }

        public CornerPlayersViewModel CornerPlayers { get; }

        private static string BuildPackUri(string relativePath)
        {
            return string.Format("pack://application:,,,/{0}", relativePath);
        }

        public ObservableCollection<PlayerTokenViewModel> PlayerTokens
        {
            get { return _tokenManager.PlayerTokens; }
        }

        public ICommand RollDiceCommand
        {
            get { return _rollDiceCommand; }
        }

        public ICommand UseItemFromSlot1Command
        {
            get { return _useItemFromSlot1Command; }
        }

        public ICommand UseItemFromSlot2Command
        {
            get { return _useItemFromSlot2Command; }
        }

        public ICommand UseItemFromSlot3Command
        {
            get { return _useItemFromSlot3Command; }
        }

        public ICommand SelectTargetUserCommand
        {
            get { return _selectTargetUserCommand; }
        }

        public ICommand CancelItemUseCommand
        {
            get { return _cancelItemUseCommand; }
        }

        public ICommand SelectDiceSlot1Command
        {
            get { return _selectDiceSlot1Command; }
        }

        public ICommand SelectDiceSlot2Command
        {
            get { return _selectDiceSlot2Command; }
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
                        RaiseAllCanExecuteChanged();
                    });
            }
        }

        public string TurnTimerText
        {
            get { return _turnTimerText; }
            private set
            {
                if (string.Equals(_turnTimerText, value, StringComparison.Ordinal))
                {
                    return;
                }

                _turnTimerText = value;
                OnPropertyChanged();
            }
        }

        public bool IsTargetSelectionActive
        {
            get { return _isTargetSelectionActive; }
            private set
            {
                if (_isTargetSelectionActive == value)
                {
                    return;
                }

                _isTargetSelectionActive = value;
                OnPropertyChanged();
                RaiseAllCanExecuteChanged();
            }
        }

        public string LastItemNotification
        {
            get { return _lastItemNotification; }
            private set
            {
                if (string.Equals(_lastItemNotification, value, StringComparison.Ordinal))
                {
                    return;
                }

                _lastItemNotification = value;
                OnPropertyChanged();
            }
        }

        public bool IsDiceSlot1Selected
        {
            get { return _isDiceSlot1Selected; }
            private set
            {
                if (_isDiceSlot1Selected == value)
                {
                    return;
                }

                _isDiceSlot1Selected = value;
                OnPropertyChanged();
            }
        }

        public bool IsDiceSlot2Selected
        {
            get { return _isDiceSlot2Selected; }
            private set
            {
                if (_isDiceSlot2Selected == value)
                {
                    return;
                }

                _isDiceSlot2Selected = value;
                OnPropertyChanged();
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
            var cellCentersByIndex = boardBuildResult.CellCentersByIndex;
            var linksByStartIndex = boardBuildResult.LinksByStartIndex;
            _startCellIndex = boardBuildResult.StartCellIndex;

            Inventory = new InventoryViewModel();
            CornerPlayers = new CornerPlayersViewModel();

            ObservableCollection<PlayerTokenViewModel> playerTokens =
                new ObservableCollection<PlayerTokenViewModel>();

            _tokenManager = new PlayerTokenManager(
                playerTokens,
                cellCentersByIndex);

            _animationService = new GameBoardAnimationService(
                _tokenManager,
                linksByStartIndex,
                cellCentersByIndex,
                MapServerIndexToVisual);

            _diceAnimator = new DiceSpriteAnimator(
                BuildPackUri(DICE_IMAGE_BASE_RELATIVE_PATH + DICE_ROLL_SPRITE_RELATIVE_PATH),
                BuildPackUri(DICE_IMAGE_BASE_RELATIVE_PATH));

            _podiumBuilder = new PodiumBuilder(_logger);

            var diceSelectionCallbacks = new DiceSelectionCallbacks(
                value => _selectedDiceSlotNumber = value,
                value => IsDiceSlot1Selected = value,
                value => IsDiceSlot2Selected = value,
                value => LastItemNotification = value);

            _diceSelectionManager = new DiceSelectionManager(
                MIN_DICE_SLOT,
                MAX_DICE_SLOT,
                HasDiceInSlot,
                diceSelectionCallbacks);

            var itemUsageDependencies = new ItemUsageManagerDependencies
            {
                Inventory = Inventory,
                GetGameplayClient = () => _gameplayClient,
                Logger = _logger,
                GetIsUseItemInProgress = () => _isUseItemInProgress,
                SetIsUseItemInProgress = value => _isUseItemInProgress = value,
                GetIsTargetSelectionActive = () => IsTargetSelectionActive,
                SetIsTargetSelectionActive = value => IsTargetSelectionActive = value,
                GetPendingItemSlotNumber = () => _pendingItemSlotNumber,
                SetPendingItemSlotNumber = value => _pendingItemSlotNumber = value,
                SetLastItemNotification = value => LastItemNotification = value,
                RefreshInventoryAsync = () => Inventory.InitializeAsync(),
                SyncGameStateAsync = () => SafeSyncGameStateAsync(true),
                RaiseAllCanExecuteChanged = RaiseAllCanExecuteChanged
            };

            var itemUsageMessages = new ItemUsageMessages
            {
                UnknownErrorMessage = Lang.GameUnknownErrorText,
                UseItemFailureMessagePrefix = Lang.GameItemUseFailurePrefixText,
                UseItemUnexpectedErrorMessage = Lang.GameItemUseUnexpectedErrorText,
                GameWindowTitle = Lang.WindowTitleGameBoard
            };

            _itemUsageController = new ItemUsageManager(
                _gameId,
                _localUserId,
                itemUsageDependencies,
                itemUsageMessages);

            _useItemFromSlot1Command = new AsyncCommand(
                () => _itemUsageController.PrepareItemTargetSelectionAsync(
                    ITEM_SLOT_1,
                    Lang.GameSelectTargetPlayerText),
                CanUseItem);

            _useItemFromSlot2Command = new AsyncCommand(
                () => _itemUsageController.PrepareItemTargetSelectionAsync(
                    ITEM_SLOT_2,
                    Lang.GameSelectTargetPlayerText),
                CanUseItem);

            _useItemFromSlot3Command = new AsyncCommand(
                () => _itemUsageController.PrepareItemTargetSelectionAsync(
                    ITEM_SLOT_3,
                    Lang.GameSelectTargetPlayerText),
                CanUseItem);

            _selectDiceSlot1Command = new RelayCommand<int>(
                _ => _diceSelectionManager.SelectSlot(MIN_DICE_SLOT),
                _ => _diceSelectionManager.CanSelectSlot(
                    MIN_DICE_SLOT,
                    BuildDiceSelectionState()));

            _selectDiceSlot2Command = new RelayCommand<int>(
                _ => _diceSelectionManager.SelectSlot(MAX_DICE_SLOT),
                _ => _diceSelectionManager.CanSelectSlot(
                    MAX_DICE_SLOT,
                    BuildDiceSelectionState()));

            _selectTargetUserCommand = new RelayCommand<int>(
                async userId => await _itemUsageController
                    .OnTargetUserSelectedAsync(userId)
                    .ConfigureAwait(false),
                _ => IsTargetSelectionActive);

            _cancelItemUseCommand = new RelayCommand<int>(
                _ => _itemUsageController.CancelItemUse(Lang.GameItemUseCancelledText),
                _ => IsTargetSelectionActive);

            TurnTimerText = DEFAULT_TURN_TIMER_TEXT;

            _serverInactivityGuard = new ServerInactivityGuard(
                _logger,
                gameId,
                localUserId,
                SERVER_INACTIVITY_TIMEOUT_SECONDS,
                SERVER_INACTIVITY_CHECK_INTERVAL_SECONDS,
                Application.Current != null
                    ? Application.Current.Dispatcher
                    : Dispatcher.CurrentDispatcher);

            _serverInactivityGuard.ServerInactivityTimeoutDetected += OnServerInactivityTimeoutDetected;
            _serverInactivityGuard.Start();

            var gameStateSyncDependencies = new GameStateSynchronizerDependencies(
                gameId,
                _logger,
                () => _gameplayClient,
                MarkServerEventReceived,
                ApplyGameStateAsync,
                HandleConnectionException);

            var gameStateSyncUiConfig = new GameStateSynchronizerUiConfig(
                Lang.GameStateSyncErrorText,
                Lang.errorTitle,
                ShowMessage);

            _gameStateSynchronizer = new GameStateSynchronizer(
                gameStateSyncDependencies,
                gameStateSyncUiConfig);

            var diceRollDependencies = new DiceRollManagerDependencies
            {
                DiceSelectionManager = _diceSelectionManager,
                GameplayClientProvider = () => _gameplayClient,
                Logger = _logger,
                GetIsMyTurn = () => IsMyTurn,
                GetIsAnimating = () => _animationService.IsAnimating,
                GetIsRollRequestInProgress = () => _isRollRequestInProgress,
                GetIsUseItemInProgress = () => _isUseItemInProgress,
                GetIsTargetSelectionActive = () => IsTargetSelectionActive,
                SetIsRollRequestInProgress = value => _isRollRequestInProgress = value,
                RaiseAllCanExecuteChanged = RaiseAllCanExecuteChanged,
                SyncGameStateAsync = () => SafeSyncGameStateAsync(false),
                InitializeInventoryAsync = SafeInitializeInventoryAsync,
                MarkServerEventReceived = MarkServerEventReceived,
                HandleConnectionException = HandleConnectionException,
                ShowMessage = ShowMessage
            };

            var diceRollMessages = new DiceRollMessages
            {
                UnknownErrorMessage = Lang.GameUnknownErrorText,
                RollDiceFailureMessagePrefix = Lang.GameDiceRollFailurePrefixText,
                RollDiceUnexpectedErrorMessage = Lang.GameDiceRollUnexpectedErrorText,
                GameWindowTitle = Lang.WindowTitleGameBoard
            };

            _diceRollManager = new DiceRollManager(
                gameId,
                localUserId,
                diceRollDependencies,
                diceRollMessages);

            _rollDiceCommand = new AsyncCommand(
                () => _diceRollManager.RollDiceForLocalPlayerAsync(),
                () => _diceRollManager.CanRollDice());

            var eventsHandler = new GameplayEventsHandler(
                _animationService,
                _diceAnimator,
                _rollDiceCommand,
                _logger,
                _localUserId,
                UpdateTurnFromState);

            var routerDependencies = new GameplayServerEventsRouterDependencies(
                eventsHandler,
                _gameStateSynchronizer,
                Inventory,
                _logger);

            var routerUiConfig = new GameplayServerEventsRouterUiConfig(
                MarkServerEventReceived,
                ShowMessage,
                value => LastItemNotification = value,
                seconds => UpdateTurnTimerText(seconds),
                Lang.GameTimeoutSkipTurnText,
                Lang.GameTimeoutKickPlayerText,
                Lang.WindowTitleGameBoard);

            _serverEventsRouter = new GameplayServerEventsRouter(
                routerDependencies,
                routerUiConfig);
        }

        public Task InitializeInventoryAsync()
        {
            return Inventory.InitializeAsync();
        }

        public async Task InitializeGameplayAsync(
            IGameplayClient client,
            string currentUserName)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _gameplayClient = client;

            string safeUserName = string.IsNullOrWhiteSpace(currentUserName)
                ? string.Format(Lang.PodiumDefaultPlayerNameFmt, _localUserId)
                : currentUserName.Trim();

            await JoinGameplayAsync(safeUserName).ConfigureAwait(false);
            await SafeSyncGameStateAsync(true).ConfigureAwait(false);
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

        public void InitializeCornerPlayers(IList<LobbyMemberViewModel> members)
        {
            if (members == null)
            {
                return;
            }

            _lobbyMembers.Clear();
            _userNamesById.Clear();

            foreach (LobbyMemberViewModel member in members)
            {
                member.IsLocalPlayer = member.UserId == _localUserId;

                _lobbyMembers.Add(member);
                _userNamesById[member.UserId] = member.UserName ?? string.Empty;
            }

            CornerPlayers.InitializeFromLobbyMembers(members);
        }

        public string ResolveUserDisplayName(int userId)
        {
            if (_userNamesById.TryGetValue(userId, out string name) &&
                !string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            return string.Format(Lang.PodiumDefaultPlayerNameFmt, userId);
        }

        public ReadOnlyCollection<PodiumPlayerViewModel> BuildPodiumPlayers(int winnerUserId)
        {
            List<PodiumPlayerViewModel> result = new List<PodiumPlayerViewModel>();

            if (_lobbyMembers.Count == 0)
            {
                return new ReadOnlyCollection<PodiumPlayerViewModel>(result);
            }

            LobbyMemberViewModel winner =
                _lobbyMembers.FirstOrDefault(m => m.UserId == winnerUserId);

            if (winner != null)
            {
                result.Add(
                    new PodiumPlayerViewModel(
                        winner.UserId,
                        winner.UserName,
                        1,
                        0));
            }

            foreach (LobbyMemberViewModel member in _lobbyMembers)
            {
                if (member.UserId == winnerUserId)
                {
                    continue;
                }

                if (result.Count >= 3)
                {
                    break;
                }

                int position = result.Count + 1;

                result.Add(
                    new PodiumPlayerViewModel(
                        member.UserId,
                        member.UserName,
                        position,
                        0));
            }

            return new ReadOnlyCollection<PodiumPlayerViewModel>(result);
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
                _tokenManager.CreateFromLobbyMember(
                    lobbyMember,
                    _startCellIndex);
            }

            _tokenManager.ResetAllTokensToCell(_startCellIndex);
        }

        private DiceSelectionState BuildDiceSelectionState()
        {
            return new DiceSelectionState(
                IsMyTurn,
                _animationService.IsAnimating,
                _isRollRequestInProgress,
                _isUseItemInProgress,
                IsTargetSelectionActive);
        }

        private void UpdateTurnTimerText(int seconds)
        {
            TurnTimerText = TurnTimerTextFormatter.Format(seconds);
        }

        private async Task JoinGameplayAsync(string currentUserName)
        {
            try
            {
                await _gameplayClient
                    .JoinGameAsync(_gameId, _localUserId, currentUserName)
                    .ConfigureAwait(false);

                _logger.InfoFormat(
                    "JoinGame OK. GameId={0}, UserId={1}, UserName={2}",
                    _gameId,
                    _localUserId,
                    currentUserName);

                MarkServerEventReceived();
            }
            catch (Exception ex)
            {
                if (HandleConnectionException(
                        ex,
                        "Connection lost while joining gameplay."))
                {
                    return;
                }

                _logger.Error(Lang.GameCallbackRegistrationErrorText, ex);

                ShowMessage(
                    Lang.GameCallbackRegistrationErrorText,
                    Lang.WindowTitleGameBoard,
                    MessageBoxImage.Error);
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

        private bool CanUseItem()
        {
            return PlayerActionGuard.CanUseItem(
                IsMyTurn,
                _animationService.IsAnimating,
                _isRollRequestInProgress,
                _isUseItemInProgress,
                IsTargetSelectionActive);
        }

        private bool HasDiceInSlot(byte slotNumber)
        {
            if (Inventory == null)
            {
                return false;
            }

            switch (slotNumber)
            {
                case MIN_DICE_SLOT:
                    return Inventory.Slot1Dice != null && Inventory.Slot1Dice.Quantity > 0;

                case MAX_DICE_SLOT:
                    return Inventory.Slot2Dice != null && Inventory.Slot2Dice.Quantity > 0;

                default:
                    return false;
            }
        }

        private Task SafeSyncGameStateAsync(bool forceUpdateTokenPositions = false)
        {
            return _gameStateSynchronizer.SyncGameStateAsync(forceUpdateTokenPositions);
        }

        private Task SafeInitializeInventoryAsync()
        {
            if (Inventory == null)
            {
                return Task.CompletedTask;
            }

            return Inventory.InitializeAsync();
        }

        private async Task ApplyGameStateAsync(
            GetGameStateResponseDto stateResponse,
            bool forceUpdateTokenPositions)
        {
            if (stateResponse == null)
            {
                return;
            }

            UpdateTurnFromState(stateResponse.CurrentTurnUserId);
            UpdateTurnTimerText(stateResponse.RemainingTurnSeconds);

            ShowPodiumFromState(stateResponse);

            if (stateResponse.Tokens == null)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    UpdateTokensFromState(
                        stateResponse,
                        forceUpdateTokenPositions);
                });
        }

        private void UpdateTokensFromState(
            GetGameStateResponseDto stateResponse,
            bool forceUpdateTokenPositions)
        {
            if (stateResponse.Tokens == null)
            {
                return;
            }

            if (forceUpdateTokenPositions || PlayerTokens.Count == 0)
            {
                foreach (TokenStateDto tokenState in stateResponse.Tokens)
                {
                    int userId = tokenState.UserId;
                    int cellIndexVisual = MapServerIndexToVisual(tokenState.CellIndex);

                    PlayerTokenViewModel playerToken =
                        _tokenManager.GetOrCreateTokenForUser(userId, cellIndexVisual);

                    _tokenManager.UpdateTokenPositionFromCell(
                        playerToken,
                        cellIndexVisual);
                }
            }

            foreach (TokenStateDto tokenState in stateResponse.Tokens)
            {
                string effectsText = GameTextBuilder.BuildEffectsText(tokenState);
                CornerPlayers.UpdateEffectsText(tokenState.UserId, effectsText);
            }
        }

        private void UpdateTurnFromState(int currentTurnUserIdFromServer)
        {
            int currentTurnUserId = currentTurnUserIdFromServer;

            bool isMyTurnNow = currentTurnUserId == _localUserId;

            _logger.InfoFormat(
                "UpdateTurnFromState: _gameId={0}, _localUserId={1}, _currentTurnUserId={2}, _isMyTurn={3}",
                _gameId,
                _localUserId,
                currentTurnUserId,
                isMyTurnNow);

            IsMyTurn = isMyTurnNow;

            CornerPlayers.UpdateCurrentTurn(currentTurnUserId);
        }

        private void ShowPodiumFromState(GetGameStateResponseDto stateResponse)
        {
            if (!stateResponse.IsFinished)
            {
                return;
            }

            if (_hasGameFinished)
            {
                return;
            }

            if (PodiumRequested == null)
            {
                return;
            }

            PodiumViewModel podiumViewModel = _podiumBuilder.BuildPodium(
                stateResponse,
                CornerPlayers);

            if (podiumViewModel == null)
            {
                return;
            }

            _hasGameFinished = true;
            PodiumRequested.Invoke(podiumViewModel);
        }

        public Task HandleServerPlayerMovedAsync(PlayerMoveResultDto move)
        {
            return _serverEventsRouter.HandlePlayerMovedAsync(move);
        }

        public Task HandleServerTurnChangedAsync(TurnChangedDto turnInfo)
        {
            return _serverEventsRouter.HandleTurnChangedAsync(turnInfo);
        }

        public Task HandleServerPlayerLeftAsync(PlayerLeftDto playerLeftInfo)
        {
            return _serverEventsRouter.HandlePlayerLeftAsync(playerLeftInfo);
        }

        public Task HandleServerItemUsedAsync(ItemUsedNotificationDto notification)
        {
            return _serverEventsRouter.HandleItemUsedAsync(notification);
        }

        public Task HandleServerTurnTimerUpdatedAsync(TurnTimerUpdateDto timerInfo)
        {
            return _serverEventsRouter.HandleTurnTimerUpdatedAsync(timerInfo);
        }

        private void RaiseAllCanExecuteChanged()
        {
            if (Application.Current == null || Application.Current.Dispatcher == null)
            {
                RaiseCanExecuteChangedOnCommands();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                RaiseCanExecuteChangedOnCommands();
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(RaiseCanExecuteChangedOnCommands));
            }
        }

        private void RaiseCanExecuteChangedOnCommands()
        {
            _rollDiceCommand.RaiseCanExecuteChanged();
            _useItemFromSlot1Command.RaiseCanExecuteChanged();
            _useItemFromSlot2Command.RaiseCanExecuteChanged();
            _useItemFromSlot3Command.RaiseCanExecuteChanged();
            _selectTargetUserCommand.RaiseCanExecuteChanged();
            _cancelItemUseCommand.RaiseCanExecuteChanged();
            _selectDiceSlot1Command.RaiseCanExecuteChanged();
            _selectDiceSlot2Command.RaiseCanExecuteChanged();
        }

        private void MarkServerEventReceived()
        {
            _serverInactivityGuard.MarkServerEventReceived();
        }

        private static void OnServerInactivityTimeoutDetected()
        {
            ConnectionLostHandlerException.HandleConnectionLost();
        }

        private static void ShowMessage(
            string message,
            string title,
            MessageBoxImage image)
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    MessageBox.Show(
                        message,
                        title,
                        MessageBoxButton.OK,
                        image);
                });
        }

        private static bool HandleConnectionException(Exception ex, string logContext)
        {
            if (!ConnectionLostHandlerException.IsConnectionException(ex))
            {
                return false;
            }

            _logger.Error(logContext, ex);
            ConnectionLostHandlerException.HandleConnectionLost();
            return true;
        }

        public void Dispose()
        {
            if (_serverInactivityGuard != null)
            {
                _serverInactivityGuard.ServerInactivityTimeoutDetected -= OnServerInactivityTimeoutDetected;
                _serverInactivityGuard.Dispose();
            }
        }

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
