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
using SnakeAndLaddersFinalProject.Managers;
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

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel : INotifyPropertyChanged, IGameplayEventsHandler
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

        private readonly int gameId;
        private readonly int localUserId;

        private readonly Dictionary<int, System.Windows.Point> cellCentersByIndex;
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

        private readonly Dictionary<int, string> userNamesById =
    new Dictionary<int, string>();

        private readonly List<LobbyMemberViewModel> lobbyMembers =
            new List<LobbyMemberViewModel>();

        // 🔹 PODIO
        private bool hasGameFinished;
        public event Action<PodiumViewModel> PodiumRequested;

        private bool hasNavigatedToPodium;

        public event Action<int, int> NavigateToPodiumRequested; // gameId, winnerUserId

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
                _ => OnDiceSlotSelected(MIN_DICE_SLOT),
                _ => CanSelectDiceSlot(MIN_DICE_SLOT));

            selectDiceSlot2Command = new RelayCommand<int>(
                _ => OnDiceSlotSelected(MAX_DICE_SLOT),
                _ => CanSelectDiceSlot(MAX_DICE_SLOT));

            selectTargetUserCommand = new RelayCommand<int>(
                async userId => await itemUsageController.OnTargetUserSelectedAsync(
                    userId,
                    UNKNOWN_ERROR_MESSAGE,
                    USE_ITEM_FAILURE_MESSAGE_PREFIX,
                    USE_ITEM_UNEXPECTED_ERROR_MESSAGE,
                    GAME_WINDOW_TITLE),
                userId => IsTargetSelectionActive);

            cancelItemUseCommand = new RelayCommand<int>(
                _ => itemUsageController.CancelItemUse(ITEM_USE_CANCELLED_MESSAGE),
                _ => IsTargetSelectionActive);

            eventsHandler = new GameplayEventsHandler(
                animationService,
                diceAnimator,
                rollDiceCommand,
                Logger,
                this.localUserId,
                UpdateTurnFromState);

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
                () => SyncGameStateAsync(true),
                RaiseAllCanExecuteChanged);

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

            return $"Jugador {userId}";
        }

        public ReadOnlyCollection<PodiumPlayerViewModel> BuildPodiumPlayers(int winnerUserId)
        {
            var result = new List<PodiumPlayerViewModel>();

            if (lobbyMembers.Count == 0)
            {
                return new ReadOnlyCollection<PodiumPlayerViewModel>(result);
            }

            // 1) Ganador
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

            // 2) Resto de jugadores (hasta 3 en total)
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

            return PlayerActionGuard.CanRollDice(
                IsMyTurn,
                animationService.IsAnimating,
                isRollRequestInProgress,
                isUseItemInProgress,
                IsTargetSelectionActive);
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

                // 👇 AVISO: ÍTEM / DADO OBTENIDO
                if (!string.IsNullOrWhiteSpace(response.GrantedItemCode) ||
                    !string.IsNullOrWhiteSpace(response.GrantedDiceCode))
                {
                    await Application.Current.Dispatcher.InvokeAsync(
                        () =>
                        {
                            string text = "¡Has obtenido ";

                            if (!string.IsNullOrWhiteSpace(response.GrantedItemCode))
                            {
                                text += $"un ítem ({response.GrantedItemCode})";
                            }

                            if (!string.IsNullOrWhiteSpace(response.GrantedDiceCode))
                            {
                                if (!string.IsNullOrWhiteSpace(response.GrantedItemCode))
                                {
                                    text += " y ";
                                }

                                text += $"un dado ({response.GrantedDiceCode})";
                            }

                            text += "!";

                            MessageBox.Show(
                                text,
                                GAME_WINDOW_TITLE,
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        });
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
            bool hasDiceInSlot = HasDiceInSlot(slotNumber);

            return PlayerActionGuard.CanSelectDiceSlot(
                IsMyTurn,
                animationService.IsAnimating,
                isRollRequestInProgress,
                isUseItemInProgress,
                IsTargetSelectionActive,
                hasDiceInSlot);
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
                UpdateTurnTimerText(stateResponse.RemainingTurnSeconds);

                // 👇 AQUÍ LLAMAMOS AL MÉTODO DEL PODIO
                ShowPodiumFromState(stateResponse);

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
                            string effectsText = GameTextBuilder.BuildEffectsText(tokenState);
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


        private async Task HandleGameFinishedAsync(GetGameStateResponseDto stateResponse)
        {
            if (stateResponse == null)
            {
                return;
            }

            if (hasNavigatedToPodium)
            {
                return;
            }

            hasNavigatedToPodium = true;

            int winnerUserId = stateResponse.WinnerUserId;

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    string message = "La partida ha terminado.";

                    if (winnerUserId > 0)
                    {
                        message = string.Format(
                            "La partida ha terminado. Ganó el jugador con Id {0}.",
                            winnerUserId);
                    }

                    NavigateToPodiumRequested?.Invoke(gameId, winnerUserId);
                });
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

        // 🔹 LÓGICA DEL PODIO
        private void ShowPodiumFromState(GetGameStateResponseDto stateResponse)
        {
            Logger.Info("ShowPodiumFromState: inicio.");

            if (stateResponse == null)
            {
                Logger.Warn("ShowPodiumFromState: stateResponse es null.");
                return;
            }

            Logger.InfoFormat(
                "ShowPodiumFromState: IsFinished={0}, Tokens={1}, WinnerUserId={2}",
                stateResponse.IsFinished,
                stateResponse.Tokens == null ? -1 : stateResponse.Tokens.Length,
                stateResponse.WinnerUserId);

            if (!stateResponse.IsFinished)
            {
                Logger.Info("ShowPodiumFromState: la partida NO está terminada todavía (IsFinished = false).");
                return;
            }

            if (hasGameFinished)
            {
                Logger.Info("ShowPodiumFromState: hasGameFinished ya era true, no se vuelve a procesar.");
                return;
            }

            if (PodiumRequested == null)
            {
                Logger.Warn("ShowPodiumFromState: nadie está suscrito a PodiumRequested.");
                return;
            }

            if (stateResponse.Tokens == null || stateResponse.Tokens.Length == 0)
            {
                Logger.Warn("ShowPodiumFromState: no hay tokens en el estado para armar el podio.");
                return;
            }

            hasGameFinished = true;

            var allMembers = new List<LobbyMemberViewModel>();

            if (CornerPlayers != null)
            {
                if (CornerPlayers.TopLeftPlayer != null)
                {
                    allMembers.Add(CornerPlayers.TopLeftPlayer);
                }

                if (CornerPlayers.TopRightPlayer != null)
                {
                    allMembers.Add(CornerPlayers.TopRightPlayer);
                }

                if (CornerPlayers.BottomLeftPlayer != null)
                {
                    allMembers.Add(CornerPlayers.BottomLeftPlayer);
                }

                if (CornerPlayers.BottomRightPlayer != null)
                {
                    allMembers.Add(CornerPlayers.BottomRightPlayer);
                }
            }

            allMembers = allMembers
                .GroupBy(m => m.UserId)
                .Select(g => g.First())
                .ToList();

            if (allMembers.Count == 0)
            {
                return;
            }

            List<TokenStateDto> orderedTokens = stateResponse.Tokens
                .OrderByDescending(t => t.CellIndex)
                .ToList();

            var podiumPlayers = new List<PodiumPlayerViewModel>();
            int position = 1;

            foreach (TokenStateDto token in orderedTokens)
            {
                LobbyMemberViewModel member = allMembers
                    .FirstOrDefault(m => m.UserId == token.UserId);

                if (member == null)
                {
                    continue;
                }

                bool isLocalPlayer = member.UserId == localUserId;

                PodiumPlayerViewModel podiumPlayer = new PodiumPlayerViewModel(
                    member.UserId,
                    member.UserName,
                    position,
                    0,
                    member.SkinImagePath);

                podiumPlayers.Add(podiumPlayer);

                position++;

                if (position > 3)
                {
                    break;
                }
            

        }

            if (podiumPlayers.Count == 0)
            {
                return;
            }

            string winnerName = "Desconocido";
            int winnerUserId = stateResponse.WinnerUserId;

            if (winnerUserId > 0)
            {
                LobbyMemberViewModel winnerMember = allMembers
                    .FirstOrDefault(m => m.UserId == winnerUserId);

                if (winnerMember != null)
                {
                    winnerName = winnerMember.UserName;
                }
            }
            else
            {
                winnerUserId = podiumPlayers[0].UserId;
                winnerName = podiumPlayers[0].UserName;
            }

            var podiumViewModel = new PodiumViewModel();
            podiumViewModel.Initialize(
                winnerUserId,
                winnerName,
                podiumPlayers.AsReadOnly());

            PodiumRequested?.Invoke(podiumViewModel);
        }

        public async Task HandleServerPlayerMovedAsync(PlayerMoveResultDto move)
        {
            if (move == null)
            {
                return;
            }

            Task handlerTask = eventsHandler.HandleServerPlayerMovedAsync(move);

            if (move.MessageIndex.HasValue)
            {
                int messageIndex = move.MessageIndex.Value;

                // antes: "{0}MessageText"
                string resourceKey = string.Format(
                    "Message{0}Text",
                    messageIndex);

                string messageText = Lang.ResourceManager.GetString(resourceKey);

                if (!string.IsNullOrWhiteSpace(messageText))
                {
                    await Application.Current.Dispatcher.InvokeAsync(
                        () =>
                        {
                            MessageBox.Show(
                                messageText,
                                GAME_WINDOW_TITLE,
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        });
                }
                else
                {
                    Logger.WarnFormat(
                        "No se encontró recurso de mensaje para key={0}.",
                        resourceKey);
                }
            }

            await handlerTask;
        }



        public async Task HandleServerTurnChangedAsync(TurnChangedDto turnInfo)
        {
            if (turnInfo == null)
            {
                return;
            }

            Task handlerTask = eventsHandler.HandleServerTurnChangedAsync(turnInfo);

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

            LastItemNotification = GameTextBuilder.BuildItemUsedMessage(notification);

            if (Inventory != null)
            {
                await Inventory.InitializeAsync();
            }

            if (gameplayClient != null)
            {
                await SyncGameStateAsync(true);
            }
        }

        public Task HandleServerTurnTimerUpdatedAsync(TurnTimerUpdateDto timerInfo)
        {
            if (timerInfo == null)
            {
                return Task.CompletedTask;
            }

            int seconds = timerInfo.RemainingSeconds;

            Application.Current.Dispatcher.Invoke(
                () => UpdateTurnTimerText(seconds));

            return Task.CompletedTask;
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
