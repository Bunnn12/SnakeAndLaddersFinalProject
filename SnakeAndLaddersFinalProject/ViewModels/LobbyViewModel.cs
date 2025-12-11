using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.LobbyService;
using SnakeAndLaddersFinalProject.Mappers;
using SnakeAndLaddersFinalProject.Policies;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels.Models;
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
        private const int MIN_VALID_USER_ID = 1;
        private const int LOBBY_CODE_MIN_LENGTH = 4;
        private const int LOBBY_CODE_MAX_LENGTH = 32;
        private const int DEFAULT_SKIN_UNLOCKED_ID = 0;
        private const string STATUS_CREATE_REQUIRES_LOGIN = "Debes iniciar sesión para " +
            "crear un lobby.";
        private const string LOBBY_CREATE_FAILED_MESSAGE = "No se pudo crear el lobby. " +
            "Intenta de nuevo más tarde.";
        private const string KICK_REASON_CODE_KICKED_BY_HOST = "KICKED_BY_HOST";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyViewModel));

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<GameBoardViewModel> NavigateToBoardRequested;
        public event Action CurrentUserKickedFromLobby;
        public event Action NavigateToMainPageRequested;

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
        private string _kickMessageForLogin = string.Empty;
        private bool _wasLastKickByHost;

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
        public bool IsCurrentUser { get; set; }

        public ObservableCollection<LobbyMemberViewModel> Members { get; }
            = new ObservableCollection<LobbyMemberViewModel>();
        public ObservableCollection<LobbySummary> PublicLobbies { get; }
            = new ObservableCollection<LobbySummary>();

        public ICommand CreateLobbyCommand { get; }
        public ICommand JoinLobbyCommand { get; }
        public ICommand JoinPublicLobbyCommand { get; }
        public ICommand StartMatchCommand { get; }
        public ICommand LeaveLobbyCommand { get; }
        public ICommand CopyInviteLinkCommand { get; }

        public string KickMessageForLogin
        {
            get { return _kickMessageForLogin; }
            private set { SetProperty(ref _kickMessageForLogin, value); }
        }

        public bool WasLastKickByHost
        {
            get { return _wasLastKickByHost; }
            private set { SetProperty(ref _wasLastKickByHost, value); }
        }

        public LobbyViewModel()
        {
            InitializeCurrentUser();

            _gameBoardClient = new GameBoardClient();
            _lobbyClient = new LobbyClient(this);
            _lobbyBoardService = new LobbyBoardService(_gameBoardClient);

            CreateLobbyCommand = new AsyncCommand(CreateLobbyAsync);
            JoinLobbyCommand = new AsyncCommand(JoinLobbyAsync, CanJoinLobby);
            JoinPublicLobbyCommand = new AsyncCommand(JoinPublicLobbyAsync,
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
        public async Task TryLeaveLobbySilentlyAsync()
        {
            if (!HasLobby())
            {
                return;
            }

            try
            {
                var client = _lobbyClient.Proxy;

                _ = client.LeaveLobby(new LeaveLobbyRequest
                {
                    PartidaId = LobbyId,
                    UserId = CurrentUserId
                });
            }
            catch (Exception ex)
            {
                _logger.Warn("Error leaving lobby silently on application close.", ex);
            }

            await Task.CompletedTask;
        }
        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        public string CodeInput
        {
            get { return _codeInput; }
            set
            {
                if (SetProperty(ref _codeInput, value))
                {
                    (JoinLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public LobbySummary SelectedPublicLobby
        {
            get { return _selectedPublicLobby; }
            set
            {
                if (SetProperty(ref _selectedPublicLobby, value))
                {
                    (JoinPublicLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsPrivateLobby
        {
            get { return _isPrivateLobby; }
            set { SetProperty(ref _isPrivateLobby, value); }
        }

        public byte MaxPlayers
        {
            get { return _maxPlayers; }
            private set
            {
                if (SetProperty(ref _maxPlayers, value))
                {
                    (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
                    UpdateStatus();
                }
            }
        }

        public bool CanStartMatch
        {
            get
            {
                return Members.Count >= AppConstants.MIN_PLAYERS_TO_START &&
                       Members.Count <= MaxPlayers &&
                       IsCurrentUserHost() &&
                       string.Equals(LobbyStatus, LobbyMessages.LOBBY_STATUS_WAITING,
                       StringComparison.OrdinalIgnoreCase);
            }
        }

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

        public bool CanCurrentUserKickMember(LobbyMemberViewModel member)
        {
            if (member == null || !HasLobby() || !IsCurrentUserHost())
            {
                return false;
            }

            if (member.UserId == CurrentUserId)
            {
                return false;
            }

            return true;
        }

        public async Task KickMemberAsync(LobbyMemberViewModel member)
        {
            if (!CanCurrentUserKickMember(member))
            {
                return;
            }

            try
            {
                var client = _lobbyClient.Proxy;
                var request = new KickPlayerFromLobbyRequest
                {
                    LobbyId = LobbyId,
                    HostUserId = CurrentUserId,
                    TargetUserId = member.UserId
                };

                await client.KickPlayerFromLobbyAsync(request);
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(ex, "LobbyViewModel.KickMemberAsync",
                    _logger);
                MessageBox.Show(userMessage, Lang.errorTitle, MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public async Task HandleLobbyUpdatedAsync(LobbyInfo lobby)
        {
            if (lobby == null)
            {
                return;
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() => ApplyLobbyInfo(lobby)));
            await Task.CompletedTask;
        }

        public async Task HandleLobbyClosedAsync(int partidaId, string reason)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (LobbyId != partidaId)
                {
                    return;
                }
                ResetLobbyState(string.IsNullOrWhiteSpace(reason) ? LobbyMessages.
                    STATUS_LOBBY_CLOSED : reason);
            }));
            await Task.CompletedTask;
        }

        public async Task HandleKickedFromLobbyAsync(int partidaId, string reason)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (LobbyId != partidaId)
                {
                    return;
                }

                bool isKickByHost = string.Equals(reason, KICK_REASON_CODE_KICKED_BY_HOST,
                    StringComparison.OrdinalIgnoreCase);
                WasLastKickByHost = isKickByHost;

                string statusMessage;
                if (isKickByHost)
                {
                    statusMessage = Lang.LobbyKickedByHostText;
                }
                else if (string.IsNullOrWhiteSpace(reason))
                {
                    statusMessage = Lang.LobbyKickedWithoutReasonText;
                }
                else
                {
                    statusMessage = string.Format(Lang.LobbyKickedWithReasonFmt, reason);
                }

                StatusText = statusMessage;
                if (!isKickByHost)
                {
                    KickMessageForLogin = statusMessage;
                }

                CurrentUserKickedFromLobby?.Invoke();
                ResetLobbyState(StatusText);
            }));
            await Task.CompletedTask;
        }

        public async Task HandlePublicLobbiesChangedAsync(IList<LobbySummary> lobbies)
        {
            _logger.InfoFormat("HandlePublicLobbiesChangedAsync: received {0} public lobbies.",
                lobbies == null ? 0 : lobbies.Count);
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
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
            await Task.CompletedTask;
        }

        private void InitializeCurrentUser()
        {
            var sessionContext = SessionContext.Current;
            if (sessionContext != null && sessionContext.UserId >= MIN_VALID_USER_ID)
            {
                CurrentUserId = sessionContext.UserId;
                CurrentUserName = string.IsNullOrWhiteSpace(sessionContext.UserName)
                    ? Lang.ProfileUnknownUserNameText
                    : sessionContext.UserName.Trim();
                return;
            }

            string fallbackName = string.Format("{0}-{1}-{2}", Lang.UiGuestNamePrefix,
                Environment.UserName, Process.GetCurrentProcess().Id);
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
            return LobbyMembershipService.IsCurrentUserHost(CurrentUserId, CurrentUserName,
                HostUserId, HostUserName, Members);
        }

        private void NotifyStartAvailabilityChanged()
        {
            OnPropertyChanged(nameof(CanStartMatch));
            (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }

        private async Task CreateLobbyAsync()
        {
            if (CurrentUserId < MIN_VALID_USER_ID)
            {
                StatusText = STATUS_CREATE_REQUIRES_LOGIN;
                return;
            }

            try
            {
                var client = _lobbyClient.Proxy;
                var boardSize = _createOptions?.BoardSize ?? BoardSizeOption.TenByTen;
                var difficultyOption = _createOptions?.Difficulty ?? DifficultyOption.Medium;
                var specialTiles = _createOptions?.SpecialTiles ?? SpecialTileOptions.None;
                var playersRequested = (byte)(_createOptions?.Players ?? AppConstants.
                    MIN_PLAYERS_TO_START);
                bool isPrivate = _createOptions != null ? _createOptions.IsPrivate : IsPrivateLobby;

                var session = SessionContext.Current;
                var profilePhotoId = session?.ProfilePhotoId;
                var currentSkinId = session?.CurrentSkinId;
                var currentSkinUnlockedId = session?.CurrentSkinUnlockedId ?? DEFAULT_SKIN_UNLOCKED_ID;

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

                if (!IsValidCreatedLobby(response))
                {
                    StatusText = LobbyMessages.STATUS_CREATE_ERROR_PREFIX + LOBBY_CREATE_FAILED_MESSAGE;
                    MessageBox.Show(LOBBY_CREATE_FAILED_MESSAGE, Lang.errorTitle, MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    ResetLobbyState(StatusText);
                    return;
                }

                ApplyCreatedLobby(response);
            }
            catch (Exception ex)
            {
                string userMessage = ExceptionHandler.Handle(ex, "LobbyViewModel.CreateLobbyAsync",
                    _logger);
                MessageBox.Show(userMessage + LobbyMessages.STATUS_CREATE_ERROR_PREFIX, Lang.errorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RaiseCanExecutes();
            }
        }

        private bool CanJoinLobby()
        {
            return TryNormalizeLobbyCode(CodeInput, out _);
        }

        private static bool TryNormalizeLobbyCode(string code, out string normalizedCode)
        {
            normalizedCode = InputValidator.Normalize(code);
            return InputValidator.IsIdentifierText(normalizedCode, LOBBY_CODE_MIN_LENGTH,
                LOBBY_CODE_MAX_LENGTH);
        }

        private async Task JoinLobbyAsync()
        {
            if (!TryNormalizeLobbyCode(CodeInput, out string normalizedCode))
            {
                return;
            }

            try
            {
                var client = _lobbyClient.Proxy;
                var session = SessionContext.Current;
                var profilePhotoId = session?.ProfilePhotoId;
                var currentSkinId = session?.CurrentSkinId;
                var currentSkinUnlockedId = session?.CurrentSkinUnlockedId ??
                    DEFAULT_SKIN_UNLOCKED_ID;

                var joinResult = client.JoinLobby(new JoinLobbyRequest
                {
                    CodigoPartida = normalizedCode,
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
                    StatusText = LobbyMessages.STATUS_JOIN_FAILED_PREFIX + LobbyMessages.
                        STATUS_LOBBY_CLOSED;
                    return;
                }

                if (IsLobbyExpired(joinResult.Lobby.ExpiresAtUtc))
                {
                    StatusText = LobbyMessages.STATUS_JOIN_FAILED_PREFIX + LobbyMessages.
                        STATUS_LOBBY_CLOSED;
                    return;
                }

                ApplyLobbyInfo(joinResult.Lobby);
                StatusText = string.Format(Lang.LobbyJoinSuccessFmt, CodigoPartida, HostUserName,
                    Members.Count, MaxPlayers);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                StatusText = LobbyMessages.STATUS_JOIN_ERROR_PREFIX + ex.Message;
                _logger.Error("Error joining lobby.", ex);
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
                var client = _lobbyClient.Proxy;
                var result = client.StartMatch(new StartMatchRequest
                {
                    PartidaId = LobbyId,
                    HostUserId = CurrentUserId
                });

                StatusText = result.Message ?? (result.Success ? Lang.LobbyStartMatchInProgressText
                    : Lang.LobbyStartMatchFailedText);

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
                _logger.Error("Error starting match.", ex);
            }
        }

        private async Task LeaveLobbyAsync()
        {
            try
            {
                var client = _lobbyClient.Proxy;
                var result = client.LeaveLobby(new LeaveLobbyRequest
                {
                    PartidaId = LobbyId,
                    UserId = CurrentUserId
                });

                var message = result.Message ?? LobbyMessages.STATUS_LEAVE_DEFAULT;
                ResetLobbyState(message);
                NavigateToMainPageRequested?.Invoke();
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                StatusText = LobbyMessages.STATUS_LEAVE_ERROR_PREFIX + ex.Message;
                _logger.Error("Error leaving lobby.", ex);
            }
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
                _logger.Error("Error copying code to clipboard.", ex);
            }
        }

        private static bool IsValidCreatedLobby(CreateGameResponse response)
        {
            if (response == null) return false;
            if (response.PartidaId <= LobbyMessages.LOBBY_ID_NOT_SET) return false;
            if (string.IsNullOrWhiteSpace(response.CodigoPartida)) return false;
            return true;
        }

        private void ApplyCreatedLobby(CreateGameResponse response)
        {
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
            var currentSkinUnlockedId = session?.CurrentSkinUnlockedId ?? DEFAULT_SKIN_UNLOCKED_ID;

            Members.Clear();
            Members.Add(new LobbyMemberViewModel(
                CurrentUserId,
                CurrentUserName,
                true,
                DateTime.Now,
                avatarId,
                currentSkinId,
                currentSkinUnlockedId));

            StatusText = string.Format(Lang.LobbyCreatedStatusFmt, CodigoPartida, MaxPlayers,
                ExpiresAtUtc.ToString("HH:mm"));
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
                StatusText = Lang.LobbyKickedWithoutReasonText;
                KickMessageForLogin = Lang.LobbyKickedWithoutReasonText;
                CurrentUserKickedFromLobby?.Invoke();
                ResetLobbyState(StatusText);
                return;
            }

            var boardSize = LobbyMapper.MapBoardSize(info.BoardSide);
            var difficultyOption = LobbyMapper.MapDifficultyFromServer(info.Difficulty);
            var specialTiles = LobbyMapper.MapSpecialTiles(info.SpecialTiles);
            var playersRequested = info.PlayersRequested > 0 ? info.PlayersRequested :
                (byte)AppConstants.MIN_PLAYERS_TO_START;

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

            _logger.InfoFormat("ApplyLobbyInfo: LobbyId={0}, Status={1}, CurrentUserId={2}," +
                " HostUserId={3}, Members={4}",
                LobbyId, LobbyStatus, CurrentUserId, HostUserId, Members.Count);

            _ = TryNavigateToBoardIfMatchStartedAsync();
        }

        private async Task TryNavigateToBoardIfMatchStartedAsync()
        {
            if (_hasNavigatedToBoard || _isTryingNavigateToBoard)
            {
                return;
            }

            if (IsCurrentUserHost())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(LobbyStatus) || !string.Equals(LobbyStatus,
                LobbyMessages.LOBBY_STATUS_IN_MATCH, StringComparison.OrdinalIgnoreCase))
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
                _logger.Info("TryNavigateToBoardIfMatchStartedAsync: triggering" +
                    " NavigateToBoardRequested.");

                Application.Current.Dispatcher.Invoke(()
                    => NavigateToBoardRequested?.Invoke(boardViewModel));
            }
            catch (Exception ex)
            {
                _logger.Error("Error obtaining board when match started.", ex);
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName]
        string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
