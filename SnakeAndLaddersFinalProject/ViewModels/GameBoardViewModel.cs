using log4net;
using SnakeAndLaddersFinalProject.Animation;
using SnakeAndLaddersFinalProject.Game;
using SnakeAndLaddersFinalProject.Game.Board;
using SnakeAndLaddersFinalProject.Game.Gameplay;
using SnakeAndLaddersFinalProject.Game.Inventory;
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardViewModel));

        private const string UNKNOWN_ERROR_MESSAGE = "Unknown error.";
        private const string GAME_WINDOW_TITLE = "Juego";

        private const byte MIN_DICE_SLOT = 1;
        private const byte MAX_DICE_SLOT = 2;

        private const byte ITEM_SLOT_1 = 1;
        private const byte ITEM_SLOT_2 = 2;
        private const byte ITEM_SLOT_3 = 3;

        private const string ROLL_DICE_FAILURE_MESSAGE_PREFIX = "No se pudo tirar el dado: ";
        private const string ROLL_DICE_UNEXPECTED_ERROR_MESSAGE = "Ocurrió un error inesperado al tirar el dado.";
        private const string GAMEPLAY_CALLBACK_ERROR_LOG_MESSAGE = "Error al registrarse para callbacks de gameplay.";
        private const string GAME_STATE_SYNC_ERROR_LOG_MESSAGE = "Error al sincronizar el estado de la partida.";
        private const string USE_ITEM_FAILURE_MESSAGE_PREFIX = "No se pudo usar el ítem: ";
        private const string USE_ITEM_UNEXPECTED_ERROR_MESSAGE = "Ocurrió un error al usar el ítem.";

        private const string SELECT_TARGET_PLAYER_MESSAGE = "Selecciona al jugador objetivo haciendo clic en su avatar.";
        private const string ITEM_USE_CANCELLED_MESSAGE = "Uso de ítem cancelado.";

        private const string DEFAULT_TURN_TIMER_TEXT = "00:30";

        private const string TIMEOUT_SKIP_MESSAGE = "Un jugador perdió su turno por tiempo.";
        private const string TIMEOUT_KICK_MESSAGE = "Un jugador fue expulsado de la partida por inactividad.";

        private const string DICE_ROLL_SPRITE_PATH = "pack://application:,,,/Assets/Images/Dice/DiceSpriteSheet.png";
        private const string DICE_FACE_BASE_PATH = "pack://application:,,,/Assets/Images/Dice/";

        private const int SERVER_INACTIVITY_TIMEOUT_SECONDS = 45;
        private const int SERVER_INACTIVITY_CHECK_INTERVAL_SECONDS = 5;

        private readonly int gameId;
        private readonly int localUserId;

        private readonly Dictionary<int, Point> cellCentersByIndex;
        private readonly Dictionary<int, BoardLinkDto> linksByStartIndex;

        private readonly PlayerTokenManager tokenManager;
        private readonly GameBoardAnimationService animationService;
        private readonly DiceSpriteAnimator diceAnimator;

        private readonly AsyncCommand rollDiceCommand;
        private readonly AsyncCommand useItemFromSlot1Command;
        private readonly AsyncCommand useItemFromSlot2Command;
        private readonly AsyncCommand useItemFromSlot3Command;

        private readonly GameplayEventsHandler eventsHandler;
        private readonly RelayCommand<int> selectTargetUserCommand;
        private readonly RelayCommand<int> cancelItemUseCommand;

        private readonly RelayCommand<int> selectDiceSlot1Command;
        private readonly RelayCommand<int> selectDiceSlot2Command;

        private readonly int startCellIndex;

        private readonly ItemUsageManager itemUsageController;
        private readonly PodiumBuilder podiumBuilder;
        private readonly DiceSelectionManager diceSelectionManager;
        private readonly ServerInactivityGuard serverInactivityGuard;
        private readonly GameStateSynchronizer gameStateSynchronizer;
        private readonly DiceRollManager diceRollManager;
        private readonly GameplayServerEventsRouter serverEventsRouter;

        private readonly Dictionary<int, string> userNamesById =
            new Dictionary<int, string>();

        private readonly List<LobbyMemberViewModel> lobbyMembers =
            new List<LobbyMemberViewModel>();

        private IGameplayClient gameplayClient;

        private int currentTurnUserId;
        private bool isMyTurn;

        private string turnTimerText = DEFAULT_TURN_TIMER_TEXT;

        private bool isRollRequestInProgress;

        private bool isUseItemInProgress;
        private bool isTargetSelectionActive;
        private byte? pendingItemSlotNumber;

        private byte? selectedDiceSlotNumber;
        private bool isDiceSlot1Selected;
        private bool isDiceSlot2Selected;

        private string lastItemNotification;

        private bool hasGameFinished;

        public event Action<PodiumViewModel> PodiumRequested;
        public event Action<int, int> NavigateToPodiumRequested;
        public event PropertyChangedEventHandler PropertyChanged;

        public int Rows { get; }

        public int Columns { get; }

        public InventoryViewModel Inventory { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }

        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }

        public CornerPlayersViewModel CornerPlayers { get; }

        public ObservableCollection<PlayerTokenViewModel> PlayerTokens
        {
            get { return tokenManager.PlayerTokens; }
        }

        public ICommand RollDiceCommand
        {
            get { return rollDiceCommand; }
        }

        public ICommand UseItemFromSlot1Command
        {
            get { return useItemFromSlot1Command; }
        }

        public ICommand UseItemFromSlot2Command
        {
            get { return useItemFromSlot2Command; }
        }

        public ICommand UseItemFromSlot3Command
        {
            get { return useItemFromSlot3Command; }
        }

        public ICommand SelectTargetUserCommand
        {
            get { return selectTargetUserCommand; }
        }

        public ICommand CancelItemUseCommand
        {
            get { return cancelItemUseCommand; }
        }

        public ICommand SelectDiceSlot1Command
        {
            get { return selectDiceSlot1Command; }
        }

        public ICommand SelectDiceSlot2Command
        {
            get { return selectDiceSlot2Command; }
        }

        public DiceSpriteAnimator DiceAnimator
        {
            get { return diceAnimator; }
        }

        public bool IsMyTurn
        {
            get { return isMyTurn; }
            private set
            {
                if (isMyTurn == value)
                {
                    return;
                }

                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        isMyTurn = value;
                        OnPropertyChanged();
                        RaiseAllCanExecuteChanged();
                    });
            }
        }

        public string TurnTimerText
        {
            get { return turnTimerText; }
            private set
            {
                if (string.Equals(turnTimerText, value, StringComparison.Ordinal))
                {
                    return;
                }

                turnTimerText = value;
                OnPropertyChanged();
            }
        }

        public bool IsTargetSelectionActive
        {
            get { return isTargetSelectionActive; }
            private set
            {
                if (isTargetSelectionActive == value)
                {
                    return;
                }

                isTargetSelectionActive = value;
                OnPropertyChanged();
                RaiseAllCanExecuteChanged();
            }
        }

        public string LastItemNotification
        {
            get { return lastItemNotification; }
            private set
            {
                if (string.Equals(lastItemNotification, value, StringComparison.Ordinal))
                {
                    return;
                }

                lastItemNotification = value;
                OnPropertyChanged();
            }
        }

        public bool IsDiceSlot1Selected
        {
            get { return isDiceSlot1Selected; }
            private set
            {
                if (isDiceSlot1Selected == value)
                {
                    return;
                }

                isDiceSlot1Selected = value;
                OnPropertyChanged();
            }
        }

        public bool IsDiceSlot2Selected
        {
            get { return isDiceSlot2Selected; }
            private set
            {
                if (isDiceSlot2Selected == value)
                {
                    return;
                }

                isDiceSlot2Selected = value;
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

            this.gameId = gameId;
            this.localUserId = localUserId;

            Rows = boardDefinition.Rows;
            Columns = boardDefinition.Columns;

            BoardBuildResult boardBuildResult = BoardBuilder.Build(boardDefinition);

            Cells = boardBuildResult.Cells;
            Connections = boardBuildResult.Connections;
            cellCentersByIndex = boardBuildResult.CellCentersByIndex;
            linksByStartIndex = boardBuildResult.LinksByStartIndex;
            startCellIndex = boardBuildResult.StartCellIndex;

            Inventory = new InventoryViewModel();
            CornerPlayers = new CornerPlayersViewModel();

            ObservableCollection<PlayerTokenViewModel> playerTokens =
                new ObservableCollection<PlayerTokenViewModel>();

            tokenManager = new PlayerTokenManager(
                playerTokens,
                cellCentersByIndex);

            animationService = new GameBoardAnimationService(
                tokenManager,
                linksByStartIndex,
                cellCentersByIndex,
                MapServerIndexToVisual);

            diceAnimator = new DiceSpriteAnimator(
                DICE_ROLL_SPRITE_PATH,
                DICE_FACE_BASE_PATH);

            podiumBuilder = new PodiumBuilder(Logger);

            diceSelectionManager = new DiceSelectionManager(
                MIN_DICE_SLOT,
                MAX_DICE_SLOT,
                HasDiceInSlot,
                value => selectedDiceSlotNumber = value,
                value => IsDiceSlot1Selected = value,
                value => IsDiceSlot2Selected = value,
                value => LastItemNotification = value);

            // 🔴 OJO: YA NO creamos aquí GameplayEventsHandler con rollDiceCommand = null
            // eventsHandler = new GameplayEventsHandler(... null ...);

            itemUsageController = new ItemUsageManager(
                this.gameId,
                this.localUserId,
                Inventory,
                () => gameplayClient,
                Logger,
                () => isUseItemInProgress,
                value => isUseItemInProgress = value,
                () => IsTargetSelectionActive,
                value => IsTargetSelectionActive = value,
                () => pendingItemSlotNumber,
                value => pendingItemSlotNumber = value,
                value => LastItemNotification = value,
                () => Inventory.InitializeAsync(),
                () => SafeSyncGameStateAsync(true),
                RaiseAllCanExecuteChanged);

            useItemFromSlot1Command = new AsyncCommand(
                () => itemUsageController.PrepareItemTargetSelectionAsync(
                    ITEM_SLOT_1,
                    SELECT_TARGET_PLAYER_MESSAGE),
                CanUseItem);

            useItemFromSlot2Command = new AsyncCommand(
                () => itemUsageController.PrepareItemTargetSelectionAsync(
                    ITEM_SLOT_2,
                    SELECT_TARGET_PLAYER_MESSAGE),
                CanUseItem);

            useItemFromSlot3Command = new AsyncCommand(
                () => itemUsageController.PrepareItemTargetSelectionAsync(
                    ITEM_SLOT_3,
                    SELECT_TARGET_PLAYER_MESSAGE),
                CanUseItem);

            selectDiceSlot1Command = new RelayCommand<int>(
                _ => diceSelectionManager.SelectSlot(MIN_DICE_SLOT),
                _ => diceSelectionManager.CanSelectSlot(
                    MIN_DICE_SLOT,
                    IsMyTurn,
                    animationService.IsAnimating,
                    isRollRequestInProgress,
                    isUseItemInProgress,
                    IsTargetSelectionActive));

            selectDiceSlot2Command = new RelayCommand<int>(
                _ => diceSelectionManager.SelectSlot(MAX_DICE_SLOT),
                _ => diceSelectionManager.CanSelectSlot(
                    MAX_DICE_SLOT,
                    IsMyTurn,
                    animationService.IsAnimating,
                    isRollRequestInProgress,
                    isUseItemInProgress,
                    IsTargetSelectionActive));

            selectTargetUserCommand = new RelayCommand<int>(
                async userId => await itemUsageController.OnTargetUserSelectedAsync(
                    userId,
                    UNKNOWN_ERROR_MESSAGE,
                    USE_ITEM_FAILURE_MESSAGE_PREFIX,
                    USE_ITEM_UNEXPECTED_ERROR_MESSAGE,
                    GAME_WINDOW_TITLE),
                _ => IsTargetSelectionActive);

            cancelItemUseCommand = new RelayCommand<int>(
                _ => itemUsageController.CancelItemUse(ITEM_USE_CANCELLED_MESSAGE),
                _ => IsTargetSelectionActive);

            TurnTimerText = DEFAULT_TURN_TIMER_TEXT;

            serverInactivityGuard = new ServerInactivityGuard(
                Logger,
                gameId,
                localUserId,
                SERVER_INACTIVITY_TIMEOUT_SECONDS,
                SERVER_INACTIVITY_CHECK_INTERVAL_SECONDS,
                Application.Current != null
                    ? Application.Current.Dispatcher
                    : Dispatcher.CurrentDispatcher);

            serverInactivityGuard.TimeoutDetected += OnServerInactivityTimeoutDetected;
            serverInactivityGuard.Start();

            gameStateSynchronizer = new GameStateSynchronizer(
                gameId,
                Logger,
                () => gameplayClient,
                MarkServerEventReceived,
                ApplyGameStateAsync,
                HandleConnectionException,
                GAME_STATE_SYNC_ERROR_LOG_MESSAGE,
                Lang.errorTitle,
                ShowMessage);

            diceRollManager = new DiceRollManager(
                gameId,
                localUserId,
                diceSelectionManager,
                () => gameplayClient,
                Logger,
                () => IsMyTurn,
                () => animationService.IsAnimating,
                () => isRollRequestInProgress,
                () => isUseItemInProgress,
                () => IsTargetSelectionActive,
                value => isRollRequestInProgress = value,
                RaiseAllCanExecuteChanged,
                () => SafeSyncGameStateAsync(false),
                SafeInitializeInventoryAsync,
                MarkServerEventReceived,
                HandleConnectionException,
                ShowMessage,
                UNKNOWN_ERROR_MESSAGE,
                ROLL_DICE_FAILURE_MESSAGE_PREFIX,
                ROLL_DICE_UNEXPECTED_ERROR_MESSAGE,
                GAME_WINDOW_TITLE);

            // ✅ Ahora sí creamos el comando de tirar dado, ya existe diceRollManager
            rollDiceCommand = new AsyncCommand(
                () => diceRollManager.RollDiceForLocalPlayerAsync(),
                () => diceRollManager.CanRollDice());

            // ✅ Ahora sí creamos GameplayEventsHandler con un rollDiceCommand válido
            eventsHandler = new GameplayEventsHandler(
                animationService,
                diceAnimator,
                rollDiceCommand,
                Logger,
                this.localUserId,
                UpdateTurnFromState);

            // ✅ Y después el router, usando el eventsHandler correcto
            serverEventsRouter = new GameplayServerEventsRouter(
                eventsHandler,
                gameStateSynchronizer,
                Inventory,
                Logger,
                MarkServerEventReceived,
                ShowMessage,
                value => LastItemNotification = value,
                seconds => UpdateTurnTimerText(seconds),
                TIMEOUT_SKIP_MESSAGE,
                TIMEOUT_KICK_MESSAGE,
                GAME_WINDOW_TITLE);
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

            gameplayClient = client;

            string safeUserName = string.IsNullOrWhiteSpace(currentUserName)
                ? string.Format("User {0}", localUserId)
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

            lobbyMembers.Clear();
            userNamesById.Clear();

            foreach (LobbyMemberViewModel member in members)
            {
                member.IsLocalPlayer = member.UserId == localUserId;

                lobbyMembers.Add(member);
                userNamesById[member.UserId] = member.UserName ?? string.Empty;
            }

            CornerPlayers.InitializeFromLobbyMembers(members);
        }

        public string ResolveUserDisplayName(int userId)
        {
            if (userNamesById.TryGetValue(userId, out string name) &&
                !string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            return string.Format("Jugador {0}", userId);
        }

        public ReadOnlyCollection<PodiumPlayerViewModel> BuildPodiumPlayers(int winnerUserId)
        {
            List<PodiumPlayerViewModel> result = new List<PodiumPlayerViewModel>();

            if (lobbyMembers.Count == 0)
            {
                return new ReadOnlyCollection<PodiumPlayerViewModel>(result);
            }

            LobbyMemberViewModel winner =
                lobbyMembers.FirstOrDefault(m => m.UserId == winnerUserId);

            if (winner != null)
            {
                result.Add(
                    new PodiumPlayerViewModel(
                        winner.UserId,
                        winner.UserName,
                        1,
                        0));
            }

            foreach (LobbyMemberViewModel member in lobbyMembers)
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
            tokenManager.PlayerTokens.Clear();

            if (lobbyMembers == null || lobbyMembers.Count == 0)
            {
                return;
            }

            foreach (LobbyMemberViewModel lobbyMember in lobbyMembers)
            {
                tokenManager.CreateFromLobbyMember(
                    lobbyMember,
                    startCellIndex);
            }

            tokenManager.ResetAllTokensToCell(startCellIndex);
        }

        private void UpdateTurnTimerText(int seconds)
        {
            TurnTimerText = TurnTimerTextFormatter.Format(seconds);
        }

        private async Task JoinGameplayAsync(string currentUserName)
        {
            try
            {
                await gameplayClient
                    .JoinGameAsync(gameId, localUserId, currentUserName)
                    .ConfigureAwait(false);

                Logger.InfoFormat(
                    "JoinGame OK. GameId={0}, UserId={1}, UserName={2}",
                    gameId,
                    localUserId,
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

                Logger.Error(GAMEPLAY_CALLBACK_ERROR_LOG_MESSAGE, ex);

                ShowMessage(
                    GAMEPLAY_CALLBACK_ERROR_LOG_MESSAGE,
                    GAME_WINDOW_TITLE,
                    MessageBoxImage.Error);
            }
        }

        private int MapServerIndexToVisual(int serverIndex)
        {
            if (serverIndex == 0)
            {
                return startCellIndex;
            }

            return serverIndex;
        }

        private bool CanUseItem()
        {
            return PlayerActionGuard.CanUseItem(
                IsMyTurn,
                animationService.IsAnimating,
                isRollRequestInProgress,
                isUseItemInProgress,
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
            return gameStateSynchronizer.SyncGameStateAsync(forceUpdateTokenPositions);
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
                        tokenManager.GetOrCreateTokenForUser(userId, cellIndexVisual);

                    tokenManager.UpdateTokenPositionFromCell(
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
            currentTurnUserId = currentTurnUserIdFromServer;

            bool isMyTurnNow = currentTurnUserId == localUserId;

            Logger.InfoFormat(
                "UpdateTurnFromState: gameId={0}, localUserId={1}, currentTurnUserId={2}, isMyTurn={3}",
                gameId,
                localUserId,
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

            if (hasGameFinished)
            {
                return;
            }

            if (PodiumRequested == null)
            {
                return;
            }

            PodiumViewModel podiumViewModel = podiumBuilder.BuildPodium(
                stateResponse,
                CornerPlayers);

            if (podiumViewModel == null)
            {
                return;
            }

            hasGameFinished = true;
            PodiumRequested.Invoke(podiumViewModel);
        }

        public Task HandleServerPlayerMovedAsync(PlayerMoveResultDto move)
        {
            return serverEventsRouter.HandlePlayerMovedAsync(move);
        }

        public Task HandleServerTurnChangedAsync(TurnChangedDto turnInfo)
        {
            return serverEventsRouter.HandleTurnChangedAsync(turnInfo);
        }

        public Task HandleServerPlayerLeftAsync(PlayerLeftDto playerLeftInfo)
        {
            return serverEventsRouter.HandlePlayerLeftAsync(playerLeftInfo);
        }

        public Task HandleServerItemUsedAsync(ItemUsedNotificationDto notification)
        {
            return serverEventsRouter.HandleItemUsedAsync(notification);
        }

        public Task HandleServerTurnTimerUpdatedAsync(TurnTimerUpdateDto timerInfo)
        {
            return serverEventsRouter.HandleTurnTimerUpdatedAsync(timerInfo);
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
            rollDiceCommand.RaiseCanExecuteChanged();
            useItemFromSlot1Command.RaiseCanExecuteChanged();
            useItemFromSlot2Command.RaiseCanExecuteChanged();
            useItemFromSlot3Command.RaiseCanExecuteChanged();
            selectTargetUserCommand.RaiseCanExecuteChanged();
            cancelItemUseCommand.RaiseCanExecuteChanged();
            selectDiceSlot1Command.RaiseCanExecuteChanged();
            selectDiceSlot2Command.RaiseCanExecuteChanged();
        }

        private void MarkServerEventReceived()
        {
            serverInactivityGuard.MarkServerEventReceived();
        }

        private void OnServerInactivityTimeoutDetected()
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

        private bool HandleConnectionException(Exception ex, string logContext)
        {
            if (!ConnectionLostHandlerException.IsConnectionException(ex))
            {
                return false;
            }

            Logger.Error(logContext, ex);
            ConnectionLostHandlerException.HandleConnectionLost();
            return true;
        }

        public void Dispose()
        {
            if (serverInactivityGuard != null)
            {
                serverInactivityGuard.TimeoutDetected -= OnServerInactivityTimeoutDetected;
                serverInactivityGuard.Dispose();
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
