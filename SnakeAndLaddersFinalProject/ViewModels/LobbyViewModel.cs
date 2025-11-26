using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.LobbyService;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class LobbyViewModel : INotifyPropertyChanged, ILobbyEventsHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LobbyViewModel));

        private const string LOBBY_ENDPOINT = "NetTcpBinding_ILobbyService";
        private const int LOBBY_ID_NOT_SET = 0;
        private const int FALLBACK_LOCAL_USER_ID = 1;
        private const int INVALID_USER_ID = 0;

        private const string STATUS_LOBBY_READY = "Lobby listo.";
        private const string STATUS_NO_LOBBY = "Sin lobby";
        private const string STATUS_LOBBY_CLOSED = "El lobby se cerró o expiró.";
        private const string STATUS_CODE_COPIED = "Código copiado al portapapeles.";
        private const string STATUS_JOIN_FAILED_PREFIX = "No se pudo entrar: ";
        private const string STATUS_CREATE_ERROR_PREFIX = "Error creando lobby: ";
        private const string STATUS_JOIN_ERROR_PREFIX = "Error al unirse: ";
        private const string STATUS_START_ERROR_PREFIX = "Error al iniciar: ";
        private const string STATUS_LEAVE_ERROR_PREFIX = "Error al salir: ";
        private const string STATUS_LEAVE_DEFAULT = "Saliste del lobby.";
        private const string STATUS_NO_VALID_PLAYERS = "No hay jugadores válidos para crear el tablero.";

        private const string LOG_BOARD_NOT_RETURNED_TEMPLATE = "El servidor no devolvió el tablero para LobbyId {0}. Intentaremos más tarde.";

        private const string LOBBY_STATUS_WAITING = "Waiting";
        private const string LOBBY_STATUS_IN_MATCH = "InMatch";

        private const string DIFFICULTY_EASY = "Easy";
        private const string DIFFICULTY_NORMAL = "Normal";
        private const string DIFFICULTY_HARD = "Hard";

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<GameBoardViewModel> NavigateToBoardRequested;
        public event Action CurrentUserKickedFromLobby;

        private readonly GameBoardClient gameBoardClient;
        private readonly LobbyClient lobbyClient;

        private string statusText = STATUS_LOBBY_READY;
        private string codigoInput = string.Empty;
        private byte maxPlayers;
        private bool isPrivateLobby;

        private bool isTryingNavigateToBoard;


        private CreateMatchOptions createOptions;
        private bool hasNavigatedToBoard;

        private LobbySummary selectedPublicLobby;

        public LobbyViewModel()
        {
            InitializeCurrentUser();

            gameBoardClient = new GameBoardClient();
            lobbyClient = new LobbyClient(this);

            CreateLobbyCommand = new AsyncCommand(CreateLobbyAsync);
            JoinLobbyCommand = new AsyncCommand(
                JoinLobbyAsync,
                () => !string.IsNullOrWhiteSpace(CodigoInput));

            JoinPublicLobbyCommand = new AsyncCommand(
                JoinPublicLobbyAsync,
                () => SelectedPublicLobby != null);

            StartMatchCommand = new AsyncCommand(StartMatchAsync, () => CanStartMatch);
            LeaveLobbyCommand = new AsyncCommand(LeaveLobbyAsync);
            CopyInviteLinkCommand = new RelayCommand(_ => CopyInviteLink());

            Members.CollectionChanged += OnMembersChanged;

            // Suscripción a lobbys públicos desde que se crea el VM
            if (CurrentUserId != INVALID_USER_ID)
            {
                lobbyClient.SubscribePublicLobbies(CurrentUserId);
            }
        }

        // --------- PROPIEDADES DE ESTADO GENERAL ---------

        public string StatusText
        {
            get { return statusText; }
            private set
            {
                if (statusText == value)
                {
                    return;
                }

                statusText = value;
                OnPropertyChanged();
            }
        }

        public string CodigoInput
        {
            get { return codigoInput; }
            set
            {
                if (codigoInput == value)
                {
                    return;
                }

                codigoInput = value;
                OnPropertyChanged();
                (JoinLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<LobbyMemberViewModel> Members { get; } =
            new ObservableCollection<LobbyMemberViewModel>();

        public ObservableCollection<LobbySummary> PublicLobbies { get; } =
            new ObservableCollection<LobbySummary>();

        public LobbySummary SelectedPublicLobby
        {
            get { return selectedPublicLobby; }
            set
            {
                if (Equals(selectedPublicLobby, value))
                {
                    return;
                }

                selectedPublicLobby = value;
                OnPropertyChanged();
                (JoinPublicLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            }
        }

        public int CurrentUserId { get; private set; }
        public string CurrentUserName { get; private set; }

        public int LobbyId { get; private set; }
        public int HostUserId { get; private set; }
        public string HostUserName { get; private set; }
        public string CodigoPartida { get; private set; }
        public string LobbyStatus { get; private set; } = LOBBY_STATUS_WAITING;
        public DateTime ExpiresAtUtc { get; private set; }

        public DifficultyOption Difficulty { get; private set; } = DifficultyOption.Medium;
        public byte PlayersRequested { get; private set; } = AppConstants.MIN_PLAYERS_TO_START;

        public bool IsPrivateLobby
        {
            get { return isPrivateLobby; }
            set
            {
                if (isPrivateLobby == value)
                {
                    return;
                }

                isPrivateLobby = value;
                OnPropertyChanged();
            }
        }

        public byte MaxPlayers
        {
            get { return maxPlayers; }
            private set
            {
                if (maxPlayers == value)
                {
                    return;
                }

                maxPlayers = value;
                OnPropertyChanged();
                (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
                UpdateStatus();
            }
        }

        // --------- COMANDOS ---------

        public ICommand CreateLobbyCommand { get; }
        public ICommand JoinLobbyCommand { get; }
        public ICommand JoinPublicLobbyCommand { get; }
        public ICommand StartMatchCommand { get; }
        public ICommand LeaveLobbyCommand { get; }
        public ICommand CopyInviteLinkCommand { get; }

        public bool CanStartMatch =>
            Members.Count >= AppConstants.MIN_PLAYERS_TO_START &&
            Members.Count <= MaxPlayers &&
            IsCurrentUserHost() &&
            string.Equals(LobbyStatus, LOBBY_STATUS_WAITING, StringComparison.OrdinalIgnoreCase);

        // --------- CONFIGURACIÓN INICIAL ---------

        public void ApplyCreateOptions(CreateMatchOptions options)
        {
            if (options == null)
            {
                return;
            }

            createOptions = options;

            Difficulty = options.Difficulty;
            PlayersRequested = (byte)options.Players;
            IsPrivateLobby = options.IsPrivate;
        }

        private void InitializeCurrentUser()
        {
            var sessionContext = SessionContext.Current;
            if (sessionContext != null && sessionContext.UserId != INVALID_USER_ID)
            {
                CurrentUserId = sessionContext.UserId;
                CurrentUserName = string.IsNullOrWhiteSpace(sessionContext.UserName)
                    ? "Unknown"
                    : sessionContext.UserName.Trim();

                return;
            }

            var fallbackName = $"Guest-{Environment.UserName}-{Process.GetCurrentProcess().Id}";
            CurrentUserName = fallbackName;
            CurrentUserId = -Math.Abs(fallbackName.GetHashCode());
        }

        private void OnMembersChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateStatus();
            NotifyStartAvailabilityChanged();
        }

        private bool IsCurrentUserHost()
        {
            foreach (var member in Members)
            {
                if (!string.IsNullOrWhiteSpace(member?.UserName) &&
                    string.Equals(member.UserName, CurrentUserName, StringComparison.OrdinalIgnoreCase))
                {
                    return member.IsHost;
                }

                if (member.UserId == CurrentUserId)
                {
                    return member.IsHost;
                }
            }

            if (!string.IsNullOrWhiteSpace(HostUserName) &&
                string.Equals(HostUserName, CurrentUserName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return HostUserId == CurrentUserId;
        }

        private void NotifyStartAvailabilityChanged()
        {
            OnPropertyChanged(nameof(CanStartMatch));
            (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }

        private LobbyServiceClient LobbyProxy => lobbyClient.Proxy;

        // --------- ACCIONES DE LOBBY: CREAR / ENTRAR / SALIR / INICIAR ---------

        private async Task CreateLobbyAsync()
        {
            try
            {
                var client = LobbyProxy;

                var boardSize = createOptions?.BoardSize ?? BoardSizeOption.TenByTen;
                var difficultyOption = createOptions?.Difficulty ?? DifficultyOption.Medium;
                var specialTiles = createOptions?.SpecialTiles ?? SpecialTileOptions.None;
                var playersRequested = (byte)(createOptions?.Players ?? AppConstants.MIN_PLAYERS_TO_START);

                // 👇 AQUÍ se decide si es privada o no
                // - Si viene de CreateMatchPage, usamos createOptions.IsPrivate
                // - Si no, usamos la propiedad IsPrivateLobby (checkbox del lobby)
                bool isPrivate = createOptions != null
                    ? createOptions.IsPrivate
                    : IsPrivateLobby;

                var session = SessionContext.Current;
                var profilePhotoId = session?.ProfilePhotoId;
                var currentSkinId = session?.CurrentSkinId;
                var currentSkinUnlockedId = session?.CurrentSkinUnlockedId ?? 0;

                var request = new CreateGameRequest
                {
                    HostUserId = CurrentUserId,
                    MaxPlayers = playersRequested,
                    Dificultad = MapDifficulty(difficultyOption),
                    TtlMinutes = AppConstants.DEFAULT_TTL_MINUTES,
                    BoardSide = (int)boardSize,
                    PlayersRequested = playersRequested,
                    SpecialTiles = specialTiles.ToString(),
                    HostAvatarId = profilePhotoId,
                    CurrentSkinId = currentSkinId,
                    CurrentSkinUnlockedId = currentSkinUnlockedId,
                    IsPrivate = isPrivate        // 👈 importante
                };

                var response = await client.CreateGameAsync(request);
                ApplyCreatedLobby(response);
            }
            catch (Exception ex)
            {
                StatusText = STATUS_CREATE_ERROR_PREFIX + ex.Message;
                Logger.Error("Error al crear lobby.", ex);
            }
            finally
            {
                RaiseCanExecutes();
            }
        }



        private async Task JoinLobbyAsync()
        {
            var code = (CodigoInput ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            try
            {
                var client = LobbyProxy;

                var session = SessionContext.Current;
                var profilePhotoId = session?.ProfilePhotoId;
                var currentSkinId = session?.CurrentSkinId;
                var currentSkinUnlockedId = session?.CurrentSkinUnlockedId ?? 0;

                var joinResult = client.JoinLobby(new JoinLobbyRequest
                {
                    CodigoPartida = code,
                    UserId = CurrentUserId,
                    UserName = CurrentUserName,
                    AvatarId = profilePhotoId,
                    CurrentSkinId = currentSkinId,
                    CurrentSkinUnlockedId = currentSkinUnlockedId
                });

                if (!joinResult.Success)
                {
                    StatusText = STATUS_JOIN_FAILED_PREFIX + joinResult.FailureReason;
                    return;
                }

                ApplyLobbyInfo(joinResult.Lobby);

                StatusText = $"Unido a {CodigoPartida}. Host: {HostUserName}. " +
                             $"{Members.Count}/{MaxPlayers}";

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                StatusText = STATUS_JOIN_ERROR_PREFIX + ex.Message;
                Logger.Error("Error al unirse al lobby.", ex);
            }
            finally
            {
                RaiseCanExecutes();
            }
        }

        private async Task JoinPublicLobbyAsync()
        {
            if (SelectedPublicLobby == null)
            {
                return;
            }

            CodigoInput = SelectedPublicLobby.CodigoPartida;
            await JoinLobbyAsync();
        }

        private async Task StartMatchAsync()
        {
            try
            {
                var client = LobbyProxy;

                var result = client.StartMatch(new StartMatchRequest
                {
                    PartidaId = LobbyId,
                    HostUserId = CurrentUserId
                });

                StatusText = result.Message ??
                             (result.Success ? "Iniciando..." : "No se pudo iniciar.");

                if (result.Success)
                {
                    hasNavigatedToBoard = true; // el host se marca como ya navegado

                    var options = createOptions ?? new CreateMatchOptions
                    {
                        BoardSize = BoardSizeOption.TenByTen,
                        Difficulty = Difficulty,
                        Players = Members.Count,
                        SpecialTiles = SpecialTileOptions.None,
                        RoomKey = CodigoPartida
                    };

                    // ... resto igual


                    MapSpecialTileBooleans(
                        options.SpecialTiles,
                        out bool enableBonusCells,
                        out bool enableTrapCells,
                        out bool enableTeleportCells);

                    var playerUserIds = Members
                        .Where(m => m != null && m.UserId != INVALID_USER_ID)
                        .Select(m => m.UserId)
                        .Distinct()
                        .ToList();

                    if (playerUserIds.Count == 0)
                    {
                        Logger.Error("StartMatchAsync: no valid player IDs to create the board.");
                        StatusText = STATUS_NO_VALID_PLAYERS;
                        return;
                    }

                    var boardDto = gameBoardClient.CreateBoard(
                        LobbyId,
                        options.BoardSize,
                        enableBonusCells,
                        enableTrapCells,
                        enableTeleportCells,
                        options.Difficulty.ToString(),
                        playerUserIds);

                    int localUserId = ResolveLocalUserIdForBoard();

                    var boardViewModel = new GameBoardViewModel(
                        boardDto,
                        LobbyId,
                        localUserId,
                        CurrentUserName);

                    boardViewModel.InitializeCornerPlayers(Members);
                    boardViewModel.InitializeTokensFromLobbyMembers(Members);

                    var gameplayClient = new GameplayClient(boardViewModel);

                    await boardViewModel.InitializeGameplayAsync(
                        gameplayClient,
                        CurrentUserName);

                    hasNavigatedToBoard = true;
                    NavigateToBoardRequested?.Invoke(boardViewModel);
                }
            }
            catch (Exception ex)
            {
                StatusText = STATUS_START_ERROR_PREFIX + ex.Message;
                Logger.Error("Error al iniciar la partida.", ex);
            }
        }

        private async Task LeaveLobbyAsync()
        {
            try
            {
                var client = LobbyProxy;

                var result = client.LeaveLobby(new LeaveLobbyRequest
                {
                    PartidaId = LobbyId,
                    UserId = CurrentUserId
                });

                var message = result.Message ?? STATUS_LEAVE_DEFAULT;

                ResetLobbyState(message);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                StatusText = STATUS_LEAVE_ERROR_PREFIX + ex.Message;
                Logger.Error("Error al salir del lobby.", ex);
            }
        }

        private static void MapSpecialTileBooleans(
            SpecialTileOptions options,
            out bool enableBonus,
            out bool enableTrap,
            out bool enableTeleport)
        {
            enableBonus = options.HasFlag(SpecialTileOptions.Dice);
            enableTrap = options.HasFlag(SpecialTileOptions.Trap);
            enableTeleport = options.HasFlag(SpecialTileOptions.Message);
        }

        private void CopyInviteLink()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(CodigoPartida))
                {
                    Clipboard.SetText(CodigoPartida);
                    StatusText = STATUS_CODE_COPIED;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error al copiar el código al portapapeles.", ex);
            }
        }

        // --------- APLICAR SNAPSHOTS DE LOBBY ---------

        private void ApplyLobbyInfo(LobbyInfo info)
        {
            if (info == null)
            {
                ResetLobbyState(STATUS_LOBBY_CLOSED);
                return;
            }

            LobbyId = info.PartidaId;
            CodigoPartida = info.CodigoPartida;
            HostUserId = info.HostUserId;
            HostUserName = info.HostUserName;
            MaxPlayers = info.MaxPlayers;
            LobbyStatus = info.Status.ToString();
            ExpiresAtUtc = info.ExpiresAtUtc;

            Members.SynchronizeWith(
                info.Players,
                (vm, dto) => vm.UserId == dto.UserId,
                dto => new LobbyMemberViewModel(
                    dto.UserId,
                    dto.UserName,
                    dto.IsHost,
                    dto.JoinedAtUtc,
                    dto.AvatarId,
                    dto.CurrentSkinId,
                    dto.CurrentSkinUnlockedId),
                (vm, dto) => vm.IsHost = dto.IsHost);

            bool isCurrentUserStillInLobby = Members.Any(m => m.UserId == CurrentUserId);

            if (!isCurrentUserStillInLobby)
            {
                StatusText = "Has sido expulsado del lobby.";
                CurrentUserKickedFromLobby?.Invoke();
                ResetLobbyState(StatusText);
                return;
            }

            var boardSize = MapBoardSize(info.BoardSide);
            var difficultyOption = MapDifficultyFromServer(info.Difficulty);
            var specialTiles = MapSpecialTiles(info.SpecialTiles);
            var playersRequested = info.PlayersRequested > 0
                ? info.PlayersRequested
                : (byte)AppConstants.MIN_PLAYERS_TO_START;

            createOptions = new CreateMatchOptions
            {
                BoardSize = boardSize,
                Difficulty = difficultyOption,
                SpecialTiles = specialTiles,
                Players = playersRequested,
                IsPrivate = info.IsPrivate,
                RoomKey = CodigoPartida
            };

            IsPrivateLobby = info.IsPrivate;

            UpdateStatus();

            Logger.InfoFormat(
                "ApplyLobbyInfo: LobbyId={0}, Status={1}, CurrentUserId={2}, HostUserId={3}, Members={4}",
                LobbyId,
                LobbyStatus,
                CurrentUserId,
                HostUserId,
                Members.Count);

            // IMPORTANTE: aquí arrancamos la navegación automática para invitados
            _ = TryNavigateToBoardIfMatchStartedAsync();
        }


        private async Task TryNavigateToBoardIfMatchStartedAsync()
        {
            if (hasNavigatedToBoard)
            {
                Logger.Info("TryNavigateToBoardIfMatchStartedAsync: ya se navegó anteriormente, se omite.");
                return;
            }

            if (isTryingNavigateToBoard)
            {
                Logger.Info("TryNavigateToBoardIfMatchStartedAsync: ya hay un intento en curso, se omite.");
                return;
            }

            bool isHost = IsCurrentUserHost();

            Logger.InfoFormat(
                "TryNavigateToBoardIfMatchStartedAsync: LobbyId={0}, Status={1}, CurrentUserId={2}, HostUserId={3}, IsHost={4}",
                LobbyId,
                LobbyStatus,
                CurrentUserId,
                HostUserId,
                isHost);

            // El host ya navega en StartMatchAsync, aquí solo invitados
            if (isHost)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(LobbyStatus))
            {
                return;
            }

            if (!string.Equals(LobbyStatus, LOBBY_STATUS_IN_MATCH, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            isTryingNavigateToBoard = true;

            try
            {
                const int maxAttempts = 5;
                const int delayMs = 800;

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    Logger.InfoFormat(
                        "TryNavigateToBoardIfMatchStartedAsync: intento {0}/{1} para obtener tablero GameId={2} (cliente UserId={3}).",
                        attempt,
                        maxAttempts,
                        LobbyId,
                        CurrentUserId);

                    var boardDto = gameBoardClient.GetBoard(LobbyId);

                    if (boardDto != null)
                    {
                        int localUserId = ResolveLocalUserIdForBoard();

                        var boardViewModel = new GameBoardViewModel(
                            boardDto,
                            LobbyId,
                            localUserId,
                            CurrentUserName);

                        boardViewModel.InitializeCornerPlayers(Members);
                        boardViewModel.InitializeTokensFromLobbyMembers(Members);

                        var gameplayClient = new GameplayClient(boardViewModel);

                        Logger.InfoFormat(
                            "TryNavigateToBoardIfMatchStartedAsync: inicializando gameplay GameId={0}, LocalUserId={1}.",
                            LobbyId,
                            localUserId);

                        await boardViewModel.InitializeGameplayAsync(
                            gameplayClient,
                            CurrentUserName).ConfigureAwait(false);

                        hasNavigatedToBoard = true;

                        Logger.Info("TryNavigateToBoardIfMatchStartedAsync: disparando NavigateToBoardRequested.");
                        Application.Current.Dispatcher.Invoke(
                            () => NavigateToBoardRequested?.Invoke(boardViewModel));

                        return;
                    }

                    // si aún no existe, esperamos un poco y reintentamos
                    Logger.WarnFormat(
                        "TryNavigateToBoardIfMatchStartedAsync: el servidor aún no tiene tablero para LobbyId {0} (intento {1}).",
                        LobbyId,
                        attempt);

                    await Task.Delay(delayMs).ConfigureAwait(false);
                }

                // después de todos los intentos, ya no insistimos
                Logger.WarnFormat(
                    "TryNavigateToBoardIfMatchStartedAsync: el servidor no devolvió el tablero para LobbyId {0} tras varios intentos.",
                    LobbyId);
            }
            catch (Exception ex)
            {
                Logger.Error("Error al obtener el tablero cuando la partida inició.", ex);
            }
            finally
            {
                if (!hasNavigatedToBoard)
                {
                    isTryingNavigateToBoard = false;
                }
            }
        }




        private int ResolveLocalUserIdForBoard()
        {
            if (CurrentUserId != INVALID_USER_ID)
            {
                return CurrentUserId;
            }

            Logger.Warn("ResolveLocalUserIdForBoard: CurrentUserId no está establecido, se usará FALLBACK_LOCAL_USER_ID.");
            return FALLBACK_LOCAL_USER_ID;
        }

        private void ApplyCreatedLobby(CreateGameResponse response)
        {
            if (response == null)
            {
                return;
            }

            // --- Datos básicos del lobby ---
            LobbyId = response.PartidaId;
            CodigoPartida = response.CodigoPartida;
            ExpiresAtUtc = response.ExpiresAtUtc;

            HostUserId = CurrentUserId;
            HostUserName = CurrentUserName;
            LobbyStatus = LOBBY_STATUS_WAITING;

            MaxPlayers = PlayersRequested;

            // --- Sincronizar createOptions (para que tenga el código correcto) ---
            if (createOptions != null)
            {
                createOptions.RoomKey = CodigoPartida;
                // createOptions.IsPrivate ya viene desde CreateMatchPage
                // y lo usamos tal cual (no lo cambiamos aquí).
            }

            // --- Miembro host local ---
            var session = SessionContext.Current;
            var avatarId = session?.ProfilePhotoId;
            var currentSkinId = session?.CurrentSkinId;
            var currentSkinUnlockedId = session?.CurrentSkinUnlockedId ?? 0;

            Members.Clear();
            Members.Add(
                new LobbyMemberViewModel(
                    CurrentUserId,
                    CurrentUserName,
                    true,
                    DateTime.Now,
                    avatarId,
                    currentSkinId,
                    currentSkinUnlockedId));

            StatusText =
                $"Lobby creado. Código {CodigoPartida}. " +
                $"Límite {MaxPlayers}. Expira {ExpiresAtUtc:HH:mm} UTC";
        }


        // --------- MAPEOS ---------

        private static string MapDifficulty(DifficultyOption value)
        {
            switch (value)
            {
                case DifficultyOption.Easy:
                    return DIFFICULTY_EASY;
                case DifficultyOption.Hard:
                    return DIFFICULTY_HARD;
                default:
                    return DIFFICULTY_NORMAL;
            }
        }

        private void RaiseCanExecutes()
        {
            (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            (JoinLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            (JoinPublicLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateStatus()
        {
            if (!HasLobby())
            {
                StatusText = STATUS_NO_LOBBY;
                return;
            }

            StatusText = $"Lobby {CodigoPartida} — Host: {HostUserName} — " +
                         $"{Members.Count}/{MaxPlayers} — {LobbyStatus}";
        }

        private bool HasLobby()
        {
            return LobbyId > LOBBY_ID_NOT_SET;
        }

        private void ResetLobbyState(string statusMessage)
        {
            Members.Clear();

            LobbyId = LOBBY_ID_NOT_SET;
            HostUserId = INVALID_USER_ID;
            HostUserName = string.Empty;
            CodigoPartida = string.Empty;
            LobbyStatus = string.Empty;
            ExpiresAtUtc = DateTime.MinValue;

            StatusText = statusMessage;
        }

        private static BoardSizeOption MapBoardSize(int boardSide)
        {
            switch (boardSide)
            {
                case 8:
                    return BoardSizeOption.EightByEight;
                case 12:
                    return BoardSizeOption.TwelveByTwelve;
                case 10:
                default:
                    return BoardSizeOption.TenByTen;
            }
        }

        private static DifficultyOption MapDifficultyFromServer(string difficulty)
        {
            if (string.Equals(difficulty, DIFFICULTY_EASY, StringComparison.OrdinalIgnoreCase))
            {
                return DifficultyOption.Easy;
            }

            if (string.Equals(difficulty, DIFFICULTY_HARD, StringComparison.OrdinalIgnoreCase))
            {
                return DifficultyOption.Hard;
            }

            return DifficultyOption.Medium;
        }

        private static SpecialTileOptions MapSpecialTiles(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return SpecialTileOptions.None;
            }

            if (Enum.TryParse(value, true, out SpecialTileOptions parsed))
            {
                return parsed;
            }

            return SpecialTileOptions.None;
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // --------- IMPLEMENTACIÓN DE CALLBACKS (ILobbyEventsHandler) ---------

        public Task HandleLobbyUpdatedAsync(LobbyInfo lobby)
        {
            if (lobby == null)
            {
                return Task.CompletedTask;
            }

            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => ApplyLobbyInfo(lobby)));

            return Task.CompletedTask;
        }

        public Task HandleLobbyClosedAsync(int partidaId, string reason)
        {
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (LobbyId != partidaId)
                    {
                        return;
                    }

                    ResetLobbyState(
                        string.IsNullOrWhiteSpace(reason)
                            ? STATUS_LOBBY_CLOSED
                            : reason);
                }));

            return Task.CompletedTask;
        }

        public Task HandleKickedFromLobbyAsync(int partidaId, string reason)
        {
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (LobbyId != partidaId)
                    {
                        return;
                    }

                    StatusText = string.IsNullOrWhiteSpace(reason)
                        ? "Has sido expulsado del lobby."
                        : $"Has sido expulsado del lobby: {reason}";

                    CurrentUserKickedFromLobby?.Invoke();
                    ResetLobbyState(StatusText);
                }));

            return Task.CompletedTask;
        }

        public Task HandlePublicLobbiesChangedAsync(IList<LobbySummary> lobbies)
        {

            Logger.InfoFormat(
        "HandlePublicLobbiesChangedAsync: recibidos {0} lobbies públicos.",
        lobbies == null ? 0 : lobbies.Count);

            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    PublicLobbies.Clear();

                    if (lobbies == null)
                    {
                        return;
                    }

                    foreach (var summary in lobbies.OrderBy(l => l.HostUserName))
                    {
                        PublicLobbies.Add(summary);
                    }
                }));

            return Task.CompletedTask;
        }
    }
}
