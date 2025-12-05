using log4net;
using SnakeAndLaddersFinalProject.Animation;
using SnakeAndLaddersFinalProject.Game;
using SnakeAndLaddersFinalProject.Game.Board;
using SnakeAndLaddersFinalProject.Game.Gameplay;
using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel : INotifyPropertyChanged, IGameplayEventsHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardViewModel));

        private const string UNKNOWN_ERROR_MESSAGE = "Unknown error.";
        private const string GAME_WINDOW_TITLE = "Juego";

        private const byte MIN_DICE_SLOT = 1;
        private const byte MAX_DICE_SLOT = 2;

        private const string ROLL_DICE_FAILURE_MESSAGE_PREFIX = "No se pudo tirar el dado: ";
        private const string ROLL_DICE_UNEXPECTED_ERROR_MESSAGE = "Ocurrió un error inesperado al tirar el dado.";
        private const string GAMEPLAY_CALLBACK_ERROR_LOG_MESSAGE = "Error al registrarse para callbacks de gameplay.";
        private const string GAME_STATE_SYNC_ERROR_LOG_MESSAGE = "Error al sincronizar el estado de la partida.";
        private const string USE_ITEM_FAILURE_MESSAGE_PREFIX = "No se pudo usar el ítem: ";
        private const string USE_ITEM_UNEXPECTED_ERROR_MESSAGE = "Ocurrió un error al usar el ítem.";

        private const string SELECT_TARGET_PLAYER_MESSAGE = "Selecciona al jugador objetivo haciendo clic en su avatar.";
        private const string ITEM_USE_CANCELLED_MESSAGE = "Uso de ítem cancelado.";

        private const int TURN_TIME_SECONDS = 120;
        private const string DEFAULT_TURN_TIMER_TEXT = "02:00";

        private const string TIMEOUT_SKIP_MESSAGE = "Un jugador perdió su turno por tiempo.";
        private const string TIMEOUT_KICK_MESSAGE = "Un jugador fue expulsado de la partida por inactividad.";


        private const string DICE_ROLL_SPRITE_PATH = "pack://application:,,,/Assets/Images/Dice/DiceSpriteSheet.png";
        private const string DICE_FACE_BASE_PATH = "pack://application:,,,/Assets/Images/Dice/";

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

        private IGameplayClient gameplayClient;

        private int currentTurnUserId;
        private bool isMyTurn;
        private readonly DispatcherTimer turnTimer;
        private int remainingTurnSeconds;

        private string turnTimerText = DEFAULT_TURN_TIMER_TEXT;

        private bool isRollRequestInProgress;

        private bool isUseItemInProgress;
        private bool isTargetSelectionActive;
        private byte? pendingItemSlotNumber;

        private byte? selectedDiceSlotNumber;
        private bool isDiceSlot1Selected;
        private bool isDiceSlot2Selected;

        private string lastItemNotification;

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

            rollDiceCommand = new AsyncCommand(
                RollDiceForLocalPlayerAsync,
                CanRollDice);

            useItemFromSlot1Command = new AsyncCommand(
                () => PrepareItemTargetSelectionAsync(1),
                CanUseItem);

            useItemFromSlot2Command = new AsyncCommand(
                () => PrepareItemTargetSelectionAsync(2),
                CanUseItem);

            useItemFromSlot3Command = new AsyncCommand(
                () => PrepareItemTargetSelectionAsync(3),
                CanUseItem);

            selectDiceSlot1Command = new RelayCommand<int>(
                _ => OnDiceSlotSelected(MIN_DICE_SLOT),
                _ => CanSelectDiceSlot(MIN_DICE_SLOT));

            selectDiceSlot2Command = new RelayCommand<int>(
                _ => OnDiceSlotSelected(MAX_DICE_SLOT),
                _ => CanSelectDiceSlot(MAX_DICE_SLOT));

            selectTargetUserCommand = new RelayCommand<int>(
                async userId => await OnTargetUserSelectedAsync(userId),
                userId => IsTargetSelectionActive);

            cancelItemUseCommand = new RelayCommand<int>(
                _ => CancelItemUse(),
                _ => IsTargetSelectionActive);

            eventsHandler = new GameplayEventsHandler(
                animationService,
                diceAnimator,
                rollDiceCommand,
                Logger,
                this.localUserId,
                UpdateTurnFromState);

            turnTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            turnTimer.Tick += OnTurnTimerTick;

            TurnTimerText = DEFAULT_TURN_TIMER_TEXT;
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

            await SyncGameStateAsync(true).ConfigureAwait(false);
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
            if (lobbyMembers == null)
            {
                return;
            }

            foreach (LobbyMemberViewModel member in lobbyMembers)
            {
                member.IsLocalPlayer = member.UserId == localUserId;
            }

            CornerPlayers.InitializeFromLobbyMembers(lobbyMembers);
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

        private void StartTurnTimer()
        {
            remainingTurnSeconds = TURN_TIME_SECONDS;
            UpdateTurnTimerText(remainingTurnSeconds);

            if (!turnTimer.IsEnabled)
            {
                turnTimer.Start();
            }
        }

        private void StopTurnTimer()
        {
            if (turnTimer.IsEnabled)
            {
                turnTimer.Stop();
            }

            remainingTurnSeconds = 0;
            UpdateTurnTimerText(remainingTurnSeconds);
        }

        private async void OnTurnTimerTick(object sender, EventArgs e)
        {
            if (remainingTurnSeconds <= 0)
            {
                StopTurnTimer();
                UpdateTurnTimerText(0);

                // Solo reportamos timeout si realmente era mi turno y tengo cliente
                if (IsMyTurn && gameplayClient != null && !isRollRequestInProgress && !isUseItemInProgress)
                {
                    try
                    {
                        Logger.InfoFormat(
                            "RegisterTurnTimeout: GameId={0}, UserId={1}",
                            gameId,
                            localUserId);

                        await gameplayClient
                            .RegisterTurnTimeoutAsync(gameId, localUserId)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error al registrar timeout de turno.", ex);
                        // No mostramos MessageBox aquí; el flujo se corrige cuando el server mande OnTurnChanged
                    }
                }

                return;
            }

            remainingTurnSeconds--;
            UpdateTurnTimerText(remainingTurnSeconds);
        }

        




        private void UpdateTurnTimerText(int seconds)
        {
            if (seconds <= 0)
            {
                TurnTimerText = "00:00";
                return;
            }

            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;

            TurnTimerText = string.Format("{0:00}:{1:00}", minutes, remainingSeconds);
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
                return startCellIndex;
            }

            return serverIndex;
        }

        private bool CanRollDice()
        {
            Logger.InfoFormat(
                "CanRollDice: gameId={0}, localUserId={1}, currentTurnUserId={2}, isMyTurn={3}, isAnimating={4}, isRollRequestInProgress={5}",
                gameId,
                localUserId,
                currentTurnUserId,
                IsMyTurn,
                animationService.IsAnimating,
                isRollRequestInProgress);

            if (!IsMyTurn)
            {
                return false;
            }

            if (animationService.IsAnimating)
            {
                return false;
            }

            if (isRollRequestInProgress)
            {
                return false;
            }

            if (isUseItemInProgress)
            {
                return false;
            }

            if (IsTargetSelectionActive)
            {
                return false;
            }

            return true;
        }

        private bool CanUseItem()
        {
            if (!IsMyTurn)
            {
                return false;
            }

            if (animationService.IsAnimating)
            {
                return false;
            }

            if (isRollRequestInProgress)
            {
                return false;
            }

            if (isUseItemInProgress)
            {
                return false;
            }

            if (IsTargetSelectionActive)
            {
                return false;
            }

            return true;
        }

        private async Task RollDiceForLocalPlayerAsync()
        {
            if (isRollRequestInProgress)
            {
                return;
            }

            isRollRequestInProgress = true;
            RaiseAllCanExecuteChanged();

            try
            {
                byte? diceSlotNumber = selectedDiceSlotNumber;

                RollDiceResponseDto response = await gameplayClient
                    .GetRollDiceAsync(gameId, localUserId, diceSlotNumber)
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

                selectedDiceSlotNumber = null;
                IsDiceSlot1Selected = false;
                IsDiceSlot2Selected = false;

                Logger.InfoFormat(
                    "RollDice request accepted. UserId={0}, From={1}, To={2}, Dice={3}",
                    localUserId,
                    response.FromCellIndex,
                    response.ToCellIndex,
                    response.DiceValue);

                if (gameplayClient != null)
                {
                    await SyncGameStateAsync().ConfigureAwait(false);
                }

                if (Inventory != null)
                {
                    await Inventory.InitializeAsync().ConfigureAwait(false);
                }
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
                isRollRequestInProgress = false;
                RaiseAllCanExecuteChanged();
            }
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

        private bool CanSelectDiceSlot(byte slotNumber)
        {
            if (!IsMyTurn)
            {
                return false;
            }

            if (animationService.IsAnimating)
            {
                return false;
            }

            if (isRollRequestInProgress)
            {
                return false;
            }

            if (isUseItemInProgress)
            {
                return false;
            }

            if (IsTargetSelectionActive)
            {
                return false;
            }

            if (!HasDiceInSlot(slotNumber))
            {
                return false;
            }

            return true;
        }

        private void OnDiceSlotSelected(byte slotNumber)
        {
            if (!HasDiceInSlot(slotNumber))
            {
                return;
            }

            selectedDiceSlotNumber = slotNumber;

            IsDiceSlot1Selected = slotNumber == MIN_DICE_SLOT;
            IsDiceSlot2Selected = slotNumber == MAX_DICE_SLOT;

            LastItemNotification = string.Format(
                "Dado del slot {0} seleccionado para el siguiente tiro.",
                slotNumber);

            RaiseAllCanExecuteChanged();
        }

        private bool HasItemInSlot(byte slotNumber)
        {
            if (Inventory == null)
            {
                return false;
            }

            switch (slotNumber)
            {
                case 1:
                    return Inventory.Slot1Item != null && Inventory.Slot1Item.Quantity > 0;

                case 2:
                    return Inventory.Slot2Item != null && Inventory.Slot2Item.Quantity > 0;

                case 3:
                    return Inventory.Slot3Item != null && Inventory.Slot3Item.Quantity > 0;

                default:
                    return false;
            }
        }

        private Task PrepareItemTargetSelectionAsync(byte slotNumber)
        {
            if (!HasItemInSlot(slotNumber))
            {
                return Task.CompletedTask;
            }

            pendingItemSlotNumber = slotNumber;
            IsTargetSelectionActive = true;

            LastItemNotification = SELECT_TARGET_PLAYER_MESSAGE;

            return Task.CompletedTask;
        }

        private async Task OnTargetUserSelectedAsync(int userId)
        {
            if (!IsTargetSelectionActive)
            {
                return;
            }

            if (!pendingItemSlotNumber.HasValue)
            {
                return;
            }

            byte slotNumber = pendingItemSlotNumber.Value;

            IsTargetSelectionActive = false;
            pendingItemSlotNumber = null;

            await UseItemAsync(slotNumber, userId);
        }

        private async Task UseItemAsync(byte slotNumber, int targetUserId)
        {
            if (isUseItemInProgress || gameplayClient == null)
            {
                return;
            }

            isUseItemInProgress = true;
            RaiseAllCanExecuteChanged();

            try
            {
                int? targetUserIdOrNull = targetUserId <= 0 ? (int?)null : targetUserId;

                UseItemResponseDto response = await gameplayClient
                    .UseItemAsync(gameId, localUserId, slotNumber, targetUserIdOrNull)
                    .ConfigureAwait(false);

                if (response == null || !response.Success)
                {
                    string failureReason = response != null && !string.IsNullOrWhiteSpace(response.FailureReason)
                        ? response.FailureReason
                        : UNKNOWN_ERROR_MESSAGE;

                    Logger.Warn("UseItem failed: " + failureReason);

                    await Application.Current.Dispatcher.InvokeAsync(
                        () =>
                        {
                            MessageBox.Show(
                                USE_ITEM_FAILURE_MESSAGE_PREFIX + failureReason,
                                GAME_WINDOW_TITLE,
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        });

                    return;
                }

                Logger.InfoFormat(
                    "UseItem OK. GameId={0}, UserId={1}, Slot={2}, TargetUserId={3}",
                    gameId,
                    localUserId,
                    slotNumber,
                    targetUserIdOrNull);
            }
            catch (FaultException faultEx)
            {
                string failureReason = string.IsNullOrWhiteSpace(faultEx.Message)
                    ? UNKNOWN_ERROR_MESSAGE
                    : faultEx.Message;

                Logger.Warn("UseItem business error: " + failureReason, faultEx);

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        MessageBox.Show(
                            USE_ITEM_FAILURE_MESSAGE_PREFIX + failureReason,
                            GAME_WINDOW_TITLE,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
            }
            catch (Exception ex)
            {
                Logger.Error(USE_ITEM_UNEXPECTED_ERROR_MESSAGE, ex);

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        MessageBox.Show(
                            USE_ITEM_UNEXPECTED_ERROR_MESSAGE,
                            GAME_WINDOW_TITLE,
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
            }
            finally
            {
                isUseItemInProgress = false;
                RaiseAllCanExecuteChanged();
            }
        }

        private void CancelItemUse()
        {
            if (!IsTargetSelectionActive && !pendingItemSlotNumber.HasValue)
            {
                return;
            }

            pendingItemSlotNumber = null;
            IsTargetSelectionActive = false;

            LastItemNotification = ITEM_USE_CANCELLED_MESSAGE;
            RaiseAllCanExecuteChanged();
        }

        private Task SyncGameStateAsync()
        {
            return SyncGameStateAsync(false);
        }

        private async Task SyncGameStateAsync(bool forceUpdateTokenPositions)
        {
            try
            {
                GetGameStateResponseDto stateResponse = await gameplayClient
                    .GetGameStateAsync(gameId)
                    .ConfigureAwait(false);

                if (stateResponse == null)
                {
                    return;
                }

                UpdateTurnFromState(stateResponse.CurrentTurnUserId);

                if (stateResponse.Tokens == null)
                {
                    return;
                }

                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
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
                            string effectsText = BuildEffectsText(tokenState);
                            CornerPlayers.UpdateEffectsText(tokenState.UserId, effectsText);
                        }
                    });
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(
                    ex,
                    string.Format(
                        "{0}.{1}",
                        nameof(GameBoardViewModel),
                        nameof(SyncGameStateAsync)),
                    Logger);

                MessageBox.Show(
                    GAME_STATE_SYNC_ERROR_LOG_MESSAGE,
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string BuildEffectsText(TokenStateDto tokenState)
        {
            var parts = new List<string>();

            if (tokenState.HasShield && tokenState.RemainingShieldTurns > 0)
            {
                parts.Add(string.Format("Escudo ({0})", tokenState.RemainingShieldTurns));
            }

            if (tokenState.RemainingFrozenTurns > 0)
            {
                parts.Add(string.Format("Congelado ({0})", tokenState.RemainingFrozenTurns));
            }

            if (tokenState.HasPendingRocketBonus)
            {
                parts.Add("Propulsor listo");
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" • ", parts);
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

        public Task HandleServerPlayerMovedAsync(PlayerMoveResultDto move)
        {
            Task handlerTask = eventsHandler.HandleServerPlayerMovedAsync(move);
            return handlerTask;
        }

        public async Task HandleServerTurnChangedAsync(TurnChangedDto turnInfo)
        {
            if (turnInfo == null)
            {
                return;
            }

            Task handlerTask = eventsHandler.HandleServerTurnChangedAsync(turnInfo);

            bool isMyTurnAfter = turnInfo.CurrentTurnUserId == localUserId;

            if (isMyTurnAfter)
            {
                StartTurnTimer();
            }
            else
            {
                StopTurnTimer();
            }

            if (!string.IsNullOrWhiteSpace(turnInfo.Reason))
            {
                string normalizedReason = turnInfo.Reason.Trim().ToUpperInvariant();

                if (normalizedReason == "TIMEOUT_SKIP")
                {
                    await Application.Current.Dispatcher.InvokeAsync(
                        () =>
                        {
                            MessageBox.Show(
                                TIMEOUT_SKIP_MESSAGE,
                                GAME_WINDOW_TITLE,
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        });
                }
                else if (normalizedReason == "TIMEOUT_KICK")
                {
                    await Application.Current.Dispatcher.InvokeAsync(
                        () =>
                        {
                            MessageBox.Show(
                                TIMEOUT_KICK_MESSAGE,
                                GAME_WINDOW_TITLE,
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        });
                }
            }

            if (gameplayClient != null)
            {
                await SyncGameStateAsync();
            }

            await handlerTask;
        }


        public Task HandleServerPlayerLeftAsync(PlayerLeftDto playerLeftInfo)
        {
            Task handlerTask = eventsHandler.HandleServerPlayerLeftAsync(playerLeftInfo);
            return handlerTask;
        }

        public async Task HandleServerItemUsedAsync(ItemUsedNotificationDto notification)
        {
            if (notification == null)
            {
                return;
            }

            Logger.InfoFormat(
                "HandleServerItemUsedAsync: GameId={0}, ItemCode={1}, UserId={2}, TargetUserId={3}",
                notification.GameId,
                notification.ItemCode,
                notification.UserId,
                notification.TargetUserId);

            LastItemNotification = BuildItemUsedMessage(notification);

            if (Inventory != null)
            {
                await Inventory.InitializeAsync();
            }

            if (gameplayClient != null)
            {
                await SyncGameStateAsync(true);
            }
        }

        private static string BuildItemUsedMessage(ItemUsedNotificationDto notification)
        {
            string actor = string.Format("Jugador {0}", notification.UserId);
            string target = notification.TargetUserId.HasValue
                ? string.Format("Jugador {0}", notification.TargetUserId.Value)
                : null;

            ItemEffectResultDto effect = notification.EffectResult;
            bool blockedByShield = effect != null && effect.WasBlockedByShield;
            bool noMovement = effect != null && effect.FromCellIndex == effect.ToCellIndex;

            switch (notification.ItemCode)
            {
                case "IT_ROCKET":
                    if (blockedByShield && target != null)
                    {
                        return string.Format(
                            "{0} intentó usar Cohete contra {1}, pero el escudo lo bloqueó.",
                            actor,
                            target);
                    }

                    return string.Format("{0} usó Cohete.", actor);

                case "IT_ANCHOR":
                    if (noMovement && target != null)
                    {
                        return string.Format(
                            "{0} intentó usar Ancla sobre {1}, pero ya está en la casilla inicial.",
                            actor,
                            target);
                    }

                    if (target == null)
                    {
                        return string.Format("{0} usó Ancla.", actor);
                    }

                    return string.Format("{0} usó Ancla contra {1}.", actor, target);

                case "IT_FREEZE":
                    if (blockedByShield && target != null)
                    {
                        return string.Format(
                            "{0} intentó congelar a {1}, pero el escudo lo bloqueó.",
                            actor,
                            target);
                    }

                    if (target == null)
                    {
                        return string.Format("{0} usó Congelar.", actor);
                    }

                    return string.Format("{0} congeló a {1}.", actor, target);

                case "IT_SHIELD":
                    return string.Format("{0} activó Escudo.", actor);

                default:
                    return string.Format("{0} usó un ítem.", actor);
            }
        }

        private void RaiseAllCanExecuteChanged()
        {
            if (Application.Current == null || Application.Current.Dispatcher == null)
            {
                rollDiceCommand.RaiseCanExecuteChanged();
                useItemFromSlot1Command.RaiseCanExecuteChanged();
                useItemFromSlot2Command.RaiseCanExecuteChanged();
                useItemFromSlot3Command.RaiseCanExecuteChanged();
                selectTargetUserCommand.RaiseCanExecuteChanged();
                cancelItemUseCommand.RaiseCanExecuteChanged();
                selectDiceSlot1Command.RaiseCanExecuteChanged();
                selectDiceSlot2Command.RaiseCanExecuteChanged();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
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
            else
            {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(
                        () =>
                        {
                            rollDiceCommand.RaiseCanExecuteChanged();
                            useItemFromSlot1Command.RaiseCanExecuteChanged();
                            useItemFromSlot2Command.RaiseCanExecuteChanged();
                            useItemFromSlot3Command.RaiseCanExecuteChanged();
                            selectTargetUserCommand.RaiseCanExecuteChanged();
                            cancelItemUseCommand.RaiseCanExecuteChanged();
                            selectDiceSlot1Command.RaiseCanExecuteChanged();
                            selectDiceSlot2Command.RaiseCanExecuteChanged();
                        }));
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
