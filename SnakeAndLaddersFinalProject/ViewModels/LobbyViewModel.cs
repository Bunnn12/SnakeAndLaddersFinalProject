using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.LobbyService;
using SnakeAndLaddersFinalProject.Mappers;

using SnakeAndLaddersFinalProject.Policies;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using SnakeAndLaddersFinalProject.Utilities;
using ServerLobbyStatus = SnakeAndLaddersFinalProject.LobbyService.LobbyStatus;
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

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class LobbyViewModel : INotifyPropertyChanged, ILobbyEventsHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyViewModel));

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<GameBoardViewModel> NavigateToBoardRequested;
        public event Action CurrentUserKickedFromLobby;

        private readonly GameBoardClient _gameBoardClient;
        private readonly LobbyClient _lobbyClient;
        private readonly LobbyBoardService _lobbyBoardService;

        private string _statusText = LobbyMessages.STATUS_LOBBY_READY;
        private string _codeInput = string.Empty;
        private byte _maxPlayers;
        private bool _isPrivateLobby;
        private bool _isTryingNavigateToBoard;

        private CreateMatchOptions _createOptions;
        private bool _hasNavigatedToBoard;

        private LobbySummary _selectedPublicLobby;

        public int CurrentUserId { get; private set; }
        public string CurrentUserName { get; private set; }

        public int LobbyId { get; private set; }
        public int HostUserId { get; private set; }
        public string HostUserName { get; private set; }
        public string CodigoPartida { get; private set; }
        public string LobbyStatus { get; private set; } = LobbyMessages.LOBBY_STATUS_WAITING;
        public DateTime ExpiresAtUtc { get; private set; }

        public DifficultyOption Difficulty { get; private set; } = DifficultyOption.Medium;
        public byte PlayersRequested { get; private set; } = AppConstants.MIN_PLAYERS_TO_START;

        public ObservableCollection<LobbyMemberViewModel> Members { get; } =
            new ObservableCollection<LobbyMemberViewModel>();

        public ObservableCollection<LobbySummary> PublicLobbies { get; } =
            new ObservableCollection<LobbySummary>();

        public ICommand CreateLobbyCommand { get; }
        public ICommand JoinLobbyCommand { get; }
        public ICommand JoinPublicLobbyCommand { get; }
        public ICommand StartMatchCommand { get; }
        public ICommand LeaveLobbyCommand { get; }
        public ICommand CopyInviteLinkCommand { get; }

        public LobbyViewModel()
        {
            InitializeCurrentUser();

            _gameBoardClient = new GameBoardClient();
            _lobbyClient = new LobbyClient(this);
            _lobbyBoardService = new LobbyBoardService(_gameBoardClient);

            CreateLobbyCommand = new AsyncCommand(CreateLobbyAsync);
            JoinLobbyCommand = new AsyncCommand(
                JoinLobbyAsync,
                () => !string.IsNullOrWhiteSpace(CodeInput));

            JoinPublicLobbyCommand = new AsyncCommand(
                JoinPublicLobbyAsync,
                () => SelectedPublicLobby != null);

            StartMatchCommand = new AsyncCommand(StartMatchAsync, () => CanStartMatch);
            LeaveLobbyCommand = new AsyncCommand(LeaveLobbyAsync);
            CopyInviteLinkCommand = new RelayCommand(_ => CopyInviteLink());

            Members.CollectionChanged += OnMembersChanged;

            if (CurrentUserId != LobbyMessages.INVALID_USER_ID)
            {
                _lobbyClient.SubscribePublicLobbies(CurrentUserId);
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            private set
            {
                if (_statusText == value)
                {
                    return;
                }

                _statusText = value;
                OnPropertyChanged();
            }
        }

        public string CodeInput
        {
            get { return _codeInput; }
            set
            {
                if (_codeInput == value)
                {
                    return;
                }

                _codeInput = value;
                OnPropertyChanged();
                (JoinLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            }
        }

        public LobbySummary SelectedPublicLobby
        {
            get { return _selectedPublicLobby; }
            set
            {
                if (Equals(_selectedPublicLobby, value))
                {
                    return;
                }

                _selectedPublicLobby = value;
                OnPropertyChanged();
                (JoinPublicLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool IsPrivateLobby
        {
            get { return _isPrivateLobby; }
            set
            {
                if (_isPrivateLobby == value)
                {
                    return;
                }

                _isPrivateLobby = value;
                OnPropertyChanged();
            }
        }

        public byte MaxPlayers
        {
            get { return _maxPlayers; }
            private set
            {
                if (_maxPlayers == value)
                {
                    return;
                }

                _maxPlayers = value;
                OnPropertyChanged();
                (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
                UpdateStatus();
            }
        }

        public bool CanStartMatch =>
            Members.Count >= AppConstants.MIN_PLAYERS_TO_START &&
            Members.Count <= MaxPlayers &&
            IsCurrentUserHost() &&
            string.Equals(
                LobbyStatus,
                LobbyMessages.LOBBY_STATUS_WAITING,
                StringComparison.OrdinalIgnoreCase);

        public void ApplyCreateOptions(CreateMatchOptions options)
        {
            if (options == null)
            {
                return;
            }

            _createOptions = options;

            Difficulty = options.Difficulty;
            PlayersRequested = (byte)options.Players;
            IsPrivateLobby = options.IsPrivate;
        }

        private void InitializeCurrentUser()
        {
            var sessionContext = SessionContext.Current;
            if (sessionContext != null && sessionContext.UserId != LobbyMessages.INVALID_USER_ID)
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
            return LobbyMembershipService.IsCurrentUserHost(
                CurrentUserId,
                CurrentUserName,
                HostUserId,
                HostUserName,
                Members);
        }

        private void NotifyStartAvailabilityChanged()
        {
            OnPropertyChanged(nameof(CanStartMatch));
            (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }

        private LobbyServiceClient LobbyProxy => _lobbyClient.Proxy;

        private async Task CreateLobbyAsync()
        {
            try
            {
                var client = LobbyProxy;

                var boardSize = _createOptions?.BoardSize ?? BoardSizeOption.TenByTen;
                var difficultyOption = _createOptions?.Difficulty ?? DifficultyOption.Medium;
                var specialTiles = _createOptions?.SpecialTiles ?? SpecialTileOptions.None;
                var playersRequested =
                    (byte)(_createOptions?.Players ?? AppConstants.MIN_PLAYERS_TO_START);

                bool isPrivate = _createOptions != null
                    ? _createOptions.IsPrivate
                    : IsPrivateLobby;

                var session = SessionContext.Current;
                var profilePhotoId = session?.ProfilePhotoId;
                var currentSkinId = session?.CurrentSkinId;
                var currentSkinUnlockedId = session?.CurrentSkinUnlockedId ?? 0;

                var request = new CreateGameRequest
                {
                    HostUserId = CurrentUserId,
                    MaxPlayers = playersRequested,
                    Dificultad = LobbyMapper.MapDifficultyToServerString(difficultyOption),
                    TtlMinutes = AppConstants.DEFAULT_EXPIRATION_MINUTES,
                    BoardSide = (int)boardSize,
                    PlayersRequested = playersRequested,
                    SpecialTiles = specialTiles.ToString(),
                    HostAvatarId = profilePhotoId,
                    CurrentSkinId = currentSkinId,
                    CurrentSkinUnlockedId = currentSkinUnlockedId,
                    IsPrivate = isPrivate
                };

                var response = await client.CreateGameAsync(request);
                ApplyCreatedLobby(response);
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(
                    ex,
                    $"{nameof(LobbyViewModel)}.{nameof(CreateLobbyAsync)}",
                    _logger);

                MessageBox.Show(
                    userMessage + "STATUS_CREATE_ERROR_PREFIX",
                    Lang.errorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                RaiseCanExecutes();
            }
        }

        private async Task JoinLobbyAsync()
        {
            var code = (CodeInput ?? string.Empty).Trim();
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
                    StatusText = LobbyMessages.STATUS_JOIN_FAILED_PREFIX + joinResult.FailureReason;
                    return;
                }

                if (joinResult.Lobby == null)
                {
                    StatusText = LobbyMessages.STATUS_JOIN_FAILED_PREFIX + LobbyMessages.STATUS_LOBBY_CLOSED;
                    return;
                }

                if (IsLobbyExpired(joinResult.Lobby.ExpiresAtUtc))
                {
                    StatusText = LobbyMessages.STATUS_JOIN_FAILED_PREFIX + LobbyMessages.STATUS_LOBBY_CLOSED;
                    return;
                }

                ApplyLobbyInfo(joinResult.Lobby);

                StatusText = $"Unido a {CodigoPartida}. Host: {HostUserName}. " +
                             $"{Members.Count}/{MaxPlayers}";

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                StatusText = LobbyMessages.STATUS_JOIN_ERROR_PREFIX + ex.Message;
                _logger.Error("Error al unirse al lobby.", ex);
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

            CodeInput = SelectedPublicLobby.CodigoPartida;
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

                if (!result.Success)
                {
                    return;
                }

                var options = _createOptions ?? new CreateMatchOptions
                {
                    BoardSize = BoardSizeOption.TenByTen,
                    Difficulty = Difficulty,
                    Players = Members.Count,
                    SpecialTiles = SpecialTileOptions.None,
                    RoomKey = CodigoPartida
                };

                var boardViewModel = await _lobbyBoardService.CreateBoardForHostAsync(
                    LobbyId,
                    CurrentUserName,
                    CurrentUserId,
                    options,
                    Members.ToList());

                if (boardViewModel == null)
                {
                    StatusText = LobbyMessages.STATUS_NO_VALID_PLAYERS;
                    return;
                }

                _hasNavigatedToBoard = true;
                NavigateToBoardRequested?.Invoke(boardViewModel);
            }
            catch (Exception ex)
            {
                StatusText = LobbyMessages.STATUS_START_ERROR_PREFIX + ex.Message;
                _logger.Error("Error al iniciar la partida.", ex);
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

                var message = result.Message ?? LobbyMessages.STATUS_LEAVE_DEFAULT;

                ResetLobbyState(message);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                StatusText = LobbyMessages.STATUS_LEAVE_ERROR_PREFIX + ex.Message;
                _logger.Error("Error al salir del lobby.", ex);
            }
        }

        public async Task TryLeaveLobbySilentlyAsync()
        {
            if (!HasLobby())
            {
                return;
            }

            try
            {
                var client = LobbyProxy;

                _ = client.LeaveLobby(new LeaveLobbyRequest
                {
                    PartidaId = LobbyId,
                    UserId = CurrentUserId
                });
            }
            catch (Exception ex)
            {
                _logger.Warn("Error al abandonar el lobby al cerrar la aplicación.", ex);
            }

            await Task.CompletedTask;
        }

        private void CopyInviteLink()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(CodigoPartida))
                {
                    Clipboard.SetText(CodigoPartida);
                    StatusText = LobbyMessages.STATUS_CODE_COPIED;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error al copiar el código al portapapeles.", ex);
            }
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
            LobbyStatus = LobbyMessages.LOBBY_STATUS_WAITING;

            MaxPlayers = PlayersRequested;

            if (_createOptions != null)
            {
                _createOptions.RoomKey = CodigoPartida;
            }

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

        private void ApplyLobbyInfo(LobbyInfo info)
        {
            if (info == null)
            {
                ResetLobbyState(LobbyMessages.STATUS_LOBBY_CLOSED);
                return;
            }

            LobbyId = info.PartidaId;
            CodigoPartida = info.CodigoPartida;
            HostUserId = info.HostUserId;
            HostUserName = info.HostUserName;
            MaxPlayers = info.MaxPlayers;
            LobbyStatus = info.Status.ToString();
            ExpiresAtUtc = info.ExpiresAtUtc;

            // Si el server ya lo marca cerrado o la fecha ya pasó, sacar al usuario.
            if (info.Status == ServerLobbyStatus.Closed || IsLobbyExpired(ExpiresAtUtc))
            {
                StatusText = LobbyMessages.STATUS_LOBBY_CLOSED;
                ResetLobbyState(StatusText);
                return;
            }


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

            var boardSize = LobbyMapper.MapBoardSize(info.BoardSide);
            var difficultyOption = LobbyMapper.MapDifficultyFromServer(info.Difficulty);
            var specialTiles = LobbyMapper.MapSpecialTiles(info.SpecialTiles);
            var playersRequested = info.PlayersRequested > 0
                ? info.PlayersRequested
                : (byte)AppConstants.MIN_PLAYERS_TO_START;

            _createOptions = new CreateMatchOptions
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

            _logger.InfoFormat(
                "ApplyLobbyInfo: LobbyId={0}, Status={1}, CurrentUserId={2}, HostUserId={3}, Members={4}",
                LobbyId,
                LobbyStatus,
                CurrentUserId,
                HostUserId,
                Members.Count);

            _ = TryNavigateToBoardIfMatchStartedAsync();
        }

        private async Task TryNavigateToBoardIfMatchStartedAsync()
        {
            if (_hasNavigatedToBoard)
            {
                _logger.Info("TryNavigateToBoardIfMatchStartedAsync: ya se navegó anteriormente, se omite.");
                return;
            }

            if (_isTryingNavigateToBoard)
            {
                _logger.Info("TryNavigateToBoardIfMatchStartedAsync: ya hay un intento en curso, se omite.");
                return;
            }

            bool isHost = IsCurrentUserHost();

            _logger.InfoFormat(
                "TryNavigateToBoardIfMatchStartedAsync: LobbyId={0}, Status={1}, CurrentUserId={2}, HostUserId={3}, IsHost={4}",
                LobbyId,
                LobbyStatus,
                CurrentUserId,
                HostUserId,
                isHost);

            if (isHost)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(LobbyStatus))
            {
                return;
            }

            if (!string.Equals(
                    LobbyStatus,
                    LobbyMessages.LOBBY_STATUS_IN_MATCH,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _isTryingNavigateToBoard = true;

            try
            {
                var boardViewModel = await _lobbyBoardService.TryCreateBoardForGuestWithRetryAsync(
                    LobbyId,
                    CurrentUserName,
                    CurrentUserId,
                    Members.ToList());

                if (boardViewModel == null)
                {
                    return;
                }

                _hasNavigatedToBoard = true;

                _logger.Info("TryNavigateToBoardIfMatchStartedAsync: disparando NavigateToBoardRequested.");
                Application.Current.Dispatcher.Invoke(
                    () => NavigateToBoardRequested?.Invoke(boardViewModel));
            }
            catch (Exception ex)
            {
                _logger.Error("Error al obtener el tablero cuando la partida inició.", ex);
            }
            finally
            {
                if (!_hasNavigatedToBoard)
                {
                    _isTryingNavigateToBoard = false;
                }
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
                StatusText = LobbyMessages.STATUS_NO_LOBBY;
                return;
            }

            StatusText = LobbyMembershipService.BuildStatusText(
                CodigoPartida,
                HostUserName,
                Members.Count,
                MaxPlayers,
                LobbyStatus);
        }

        private bool HasLobby()
        {
            return LobbyId > LobbyMessages.LOBBY_ID_NOT_SET;
        }

        private static bool IsLobbyExpired(DateTime expiresAtUtc)
        {
            return expiresAtUtc <= DateTime.UtcNow;
        }

        private void ResetLobbyState(string statusMessage)
        {
            Members.Clear();

            LobbyId = LobbyMessages.LOBBY_ID_NOT_SET;
            HostUserId = LobbyMessages.INVALID_USER_ID;
            HostUserName = string.Empty;
            CodigoPartida = string.Empty;
            LobbyStatus = string.Empty;
            ExpiresAtUtc = DateTime.MinValue;

            StatusText = statusMessage;
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                            ? LobbyMessages.STATUS_LOBBY_CLOSED
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
            _logger.InfoFormat(
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
