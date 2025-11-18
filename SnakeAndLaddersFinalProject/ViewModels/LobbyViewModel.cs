using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.LobbyService;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class LobbyViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LobbyViewModel));

        private const string LOBBY_ENDPOINT = "NetTcpBinding_ILobbyService";
        private const int POLL_INTERVAL_SECONDS = 2;
        private const int LOBBY_ID_NOT_SET = 0;
        private const int FALLBACK_LOCAL_USER_ID = 1;

        private const string STATUS_LOBBY_READY = "Lobby listo.";
        private const string STATUS_NO_LOBBY = "Sin lobby";
        private const string STATUS_LOBBY_CLOSED = "El lobby se cerró o expiró.";
        private const string STATUS_CODE_COPIED = "Código copiado al portapapeles.";
        private const string STATUS_JOIN_FAILED_PREFIX = "No se pudo entrar: ";
        private const string STATUS_CREATE_ERROR_PREFIX = "Error creando lobby: ";
        private const string STATUS_JOIN_ERROR_PREFIX = "Error al unirse: ";
        private const string STATUS_REFRESH_ERROR_PREFIX = "Error al refrescar el lobby.";
        private const string STATUS_START_ERROR_PREFIX = "Error al iniciar: ";
        private const string STATUS_LEAVE_ERROR_PREFIX = "Error al salir: ";
        private const string STATUS_LEAVE_DEFAULT = "Saliste del lobby.";

        private const string LOBBY_STATUS_WAITING = "Waiting";
        private const string LOBBY_STATUS_IN_MATCH = "InMatch";

        private const string DIFFICULTY_EASY = "Easy";
        private const string DIFFICULTY_NORMAL = "Normal";
        private const string DIFFICULTY_HARD = "Hard";

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<GameBoardViewModel> NavigateToBoardRequested;
        public event Action CurrentUserKickedFromLobby;

        private readonly DispatcherTimer pollTimer =
        new DispatcherTimer { Interval = TimeSpan.FromSeconds(POLL_INTERVAL_SECONDS) };

        private readonly GameBoardClient gameBoardClient;



        private string statusText = STATUS_LOBBY_READY;
        private string codigoInput = string.Empty;
        private byte maxPlayers;

        private CreateMatchOptions createOptions;
        private bool hasNavigatedToBoard;

        public LobbyViewModel()
        {
            InitializeCurrentUser();

            gameBoardClient = new GameBoardClient();   

            CreateLobbyCommand = new AsyncCommand(CreateLobbyAsync);
            JoinLobbyCommand = new AsyncCommand(
                JoinLobbyAsync,
                () => !string.IsNullOrWhiteSpace(CodigoInput));

            StartMatchCommand = new AsyncCommand(StartMatchAsync, () => CanStartMatch);
            LeaveLobbyCommand = new AsyncCommand(LeaveLobbyAsync);
            CopyInviteLinkCommand = new RelayCommand(_ => CopyInviteLink());

            pollTimer.Tick += async (_, __) => await RefreshLobbyAsync();

            Members.CollectionChanged += OnMembersChanged;
        }


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

        public ICommand CreateLobbyCommand { get; }
        public ICommand JoinLobbyCommand { get; }
        public ICommand StartMatchCommand { get; }
        public ICommand LeaveLobbyCommand { get; }
        public ICommand CopyInviteLinkCommand { get; }

        public bool CanStartMatch =>
            Members.Count >= AppConstants.MIN_PLAYERS_TO_START &&
            Members.Count <= MaxPlayers &&
            IsCurrentUserHost() &&
            string.Equals(LobbyStatus, LOBBY_STATUS_WAITING, StringComparison.OrdinalIgnoreCase);

        public void ApplyCreateOptions(CreateMatchOptions options)
        {
            if (options == null)
            {
                return;
            }

            createOptions = options;

            Difficulty = options.Difficulty;
            PlayersRequested = (byte)options.Players;
        }

        private void InitializeCurrentUser()
        {
            var sessionContext = SessionContext.Current;
            if (sessionContext != null && sessionContext.UserId != 0)
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

        private async Task UseLobbyClientAsync(Func<LobbyServiceClient, Task> action)
        {
            using (var client = new LobbyServiceClient(LOBBY_ENDPOINT))
            {
                await action(client);
            }
        }

        private async Task CreateLobbyAsync()
        {
            try
            {
                await UseLobbyClientAsync(async client =>
                {
                    var boardSize = createOptions?.BoardSize ?? BoardSizeOption.TenByTen;
                    var difficultyOption = createOptions?.Difficulty ?? DifficultyOption.Medium;
                    var specialTiles = createOptions?.SpecialTiles ?? SpecialTileOptions.None;
                    var playersRequested = (byte)(createOptions?.Players ?? AppConstants.MIN_PLAYERS_TO_START);

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
                        CurrentSkinUnlockedId = currentSkinUnlockedId
                    };

                    var response = await client.CreateGameAsync(request);

                    ApplyCreatedLobby(response);

                    await Task.CompletedTask;
                });

                await EnsurePollingAndRefreshAsync();
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
                await UseLobbyClientAsync(async client =>
                {
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
                });

                await EnsurePollingAndRefreshAsync();
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

        private async Task RefreshLobbyAsync()
        {
            if (!HasLobby())
            {
                return;
            }

            try
            {
                await UseLobbyClientAsync(async client =>
                {
                    var info = client.GetLobbyInfo(new GetLobbyInfoRequest
                    {
                        PartidaId = LobbyId
                    });

                    if (info == null)
                    {
                        ResetLobbyState(STATUS_LOBBY_CLOSED);
                        return;
                    }

                    ApplyLobbyInfo(info);
                    await Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                Logger.Error(STATUS_REFRESH_ERROR_PREFIX, ex);
            }
            finally
            {
                RaiseCanExecutes();
            }
        }

        private async Task StartMatchAsync()
        {
            try
            {
                await UseLobbyClientAsync(async client =>
                {
                    var result = client.StartMatch(new StartMatchRequest
                    {
                        PartidaId = LobbyId,
                        HostUserId = CurrentUserId
                    });

                    StatusText = result.Message ??
                                 (result.Success ? "Iniciando..." : "No se pudo iniciar.");

                    if (result.Success)
                    {
                        var options = createOptions ?? new CreateMatchOptions
                        {
                            BoardSize = BoardSizeOption.TenByTen,
                            Difficulty = Difficulty,
                            Players = Members.Count,
                            SpecialTiles = SpecialTileOptions.None,
                            RoomKey = CodigoPartida
                        };

                        bool enableBonusCells;
                        bool enableTrapCells;
                        bool enableTeleportCells;

                        MapSpecialTileBooleans(
                            options.SpecialTiles,
                            out enableBonusCells,
                            out enableTrapCells,
                            out enableTeleportCells);
                        
                        var playerUserIds = Members
                            .Where(m => m != null && m.UserId > 0)
                            .Select(m => m.UserId)
                            .Distinct()
                            .ToList();

                        if (playerUserIds.Count == 0)
                        {
                            Logger.Error("StartMatchAsync: no valid player IDs to create the board.");
                            StatusText = "No hay jugadores válidos para crear el tablero.";
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


                        Debug.WriteLine("LobbyID:" + LobbyId);
                        Debug.WriteLine("BoardID:" + boardDto);

                        IGameplayClient gameplayClient = new GameplayClient();

                        int localUserId = ResolveLocalUserIdForBoard();

                        var boardViewModel = new GameBoardViewModel(
                            boardDto,
                            gameplayClient,
                            LobbyId,
                            localUserId);

                        boardViewModel.InitializeCornerPlayers(Members);
                        boardViewModel.InitializeTokensFromLobbyMembers(Members);

                        hasNavigatedToBoard = true;
                        NavigateToBoardRequested?.Invoke(boardViewModel);
                    }

                    await Task.CompletedTask;
                });

                await RefreshLobbyAsync();
            }
            catch (Exception ex)
            {
                StatusText = STATUS_START_ERROR_PREFIX + ex.Message;
                Logger.Error("Error al iniciar la partida.", ex);
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

        private async Task LeaveLobbyAsync()
        {
            try
            {
                await UseLobbyClientAsync(async client =>
                {
                    var result = client.LeaveLobby(new LeaveLobbyRequest
                    {
                        PartidaId = LobbyId,
                        UserId = CurrentUserId
                    });

                    var message = result.Message ?? STATUS_LEAVE_DEFAULT;

                    ResetLobbyState(message);

                    await Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                StatusText = STATUS_LEAVE_ERROR_PREFIX + ex.Message;
                Logger.Error("Error al salir del lobby.", ex);
            }
        }

        private async Task EnsurePollingAndRefreshAsync()
        {
            if (!pollTimer.IsEnabled)
            {
                pollTimer.Start();
            }

            await RefreshLobbyAsync();
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
                pollTimer.Stop();
                StatusText = "Has sido expulsado del lobby.";
                var handler = CurrentUserKickedFromLobby;
                handler?.Invoke();
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
                IsPrivate = false,
                RoomKey = CodigoPartida
            };

            UpdateStatus();

            TryNavigateToBoardIfMatchStarted();
        }

        private void TryNavigateToBoardIfMatchStarted()
        {
            if (hasNavigatedToBoard)
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

            hasNavigatedToBoard = true;

            try
            {
                var boardDto = gameBoardClient.GetBoard(LobbyId);

                if (boardDto == null)
                {
                    Logger.Warn($"El servidor no devolvió el tablero para LobbyId {LobbyId}. Intentaremos más tarde.");
                    hasNavigatedToBoard = false;
                    return;
                }

                IGameplayClient gameplayClient = new GameplayClient();

                int localUserId = ResolveLocalUserIdForBoard();

                var boardViewModel = new GameBoardViewModel(
                    boardDto,
                    gameplayClient,
                    LobbyId,
                    localUserId);

                boardViewModel.InitializeCornerPlayers(Members);
                boardViewModel.InitializeTokensFromLobbyMembers(Members);

                NavigateToBoardRequested?.Invoke(boardViewModel);
            }
            catch (Exception ex)
            {
                Logger.Error("Error al obtener el tablero cuando la partida inició.", ex);
                hasNavigatedToBoard = false;
            }
        }

        private int ResolveLocalUserIdForBoard()
        {
            if (CurrentUserId > 0)
            {
                return CurrentUserId;
            }

            LobbyMemberViewModel self = null;

            if (!string.IsNullOrWhiteSpace(CurrentUserName))
            {
                self = Members
                    .FirstOrDefault(m =>
                        !string.IsNullOrWhiteSpace(m.UserName) &&
                        string.Equals(
                            m.UserName,
                            CurrentUserName,
                            StringComparison.OrdinalIgnoreCase));
            }

            if (self == null)
            {
                self = Members.FirstOrDefault(m => m.UserId > 0);
            }

            if (self != null && self.UserId > 0)
            {
                return self.UserId;
            }

            Logger.Warn("ResolveLocalUserIdForBoard no pudo determinar un UserId válido. Se usará un valor de respaldo.");
            return FALLBACK_LOCAL_USER_ID;
        }

        private void ApplyCreatedLobby(CreateGameResponse response)
        {
            if (response == null)
            {
                return;
            }

            LobbyId = response.PartidaId;
            CodigoPartida = response.CodigoPartida;
            ExpiresAtUtc = response.ExpiresAtUtc;

            HostUserId = CurrentUserId;
            HostUserName = CurrentUserName;
            LobbyStatus = LOBBY_STATUS_WAITING;

            MaxPlayers = PlayersRequested;

            var session = SessionContext.Current;
            var avatarId = session?.ProfilePhotoId;
            var currentSkinId = session?.CurrentSkinId;
            var currentSkinUnlockedId = session?.CurrentSkinUnlockedId ?? 0;

            Members.Clear();
            Members.Add(new LobbyMemberViewModel(
                CurrentUserId,
                CurrentUserName,
                true,
                DateTime.Now,
                avatarId,
                currentSkinId,
                currentSkinUnlockedId));

            StatusText = $"Lobby creado. Código {CodigoPartida}. " +
                         $"Límite {MaxPlayers}. Expira {ExpiresAtUtc:HH:mm} UTC";
        }

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
            pollTimer.Stop();
            Members.Clear();

            LobbyId = LOBBY_ID_NOT_SET;
            HostUserId = 0;
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
                case 7:
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

            SpecialTileOptions parsed;
            if (Enum.TryParse(value, true, out parsed))
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
    }
}
