using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.Infrastructure;    
using SnakeAndLaddersFinalProject.LobbyService;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class LobbyViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LobbyViewModel));

        private const string LOBBY_ENDPOINT = "NetTcpBinding_ILobbyService";
        private const int POLL_INTERVAL_SECONDS = 2; 

        private readonly DispatcherTimer pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(POLL_INTERVAL_SECONDS) };

        private string statusText = "Lobby listo.";
        private string codigoInput = string.Empty;

        
        public BoardSizeOption BoardSize { get; private set; } = BoardSizeOption.TenByTen;
        public DifficultyOption Difficulty { get; private set; } = DifficultyOption.Medium;
        public SpecialTileOptions SpecialTiles { get; private set; } = SpecialTileOptions.None;
        public bool IsPrivate { get; private set; }
        public string RoomKey { get; private set; } = string.Empty;
        public byte PlayersRequested { get; private set; } = AppConstants.MIN_PLAYERS_TO_START;


        public string StatusText
        {
            get { return statusText; }
            private set { statusText = value; OnPropertyChanged(); }
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

        public ObservableCollection<LobbyMemberViewModel> Members { get; } = new ObservableCollection<LobbyMemberViewModel>();

        public int CurrentUserId { get; private set; }
        public string CurrentUserName { get; private set; }

        public int LobbyId { get; private set; }
        public int HostUserId { get; private set; }
        public string HostUserName { get; private set; }
        public string CodigoPartida { get; private set; }
        private byte maxPlayers;
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
                // Si tu botón "Iniciar" depende de esto:
                (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
                // Si muestras "X/Y" en StatusText:
                UpdateStatus();
            }
        }
        public string LobbyStatus { get; private set; } = "Waiting";
        public DateTime ExpiresAtUtc { get; private set; }

        public ICommand CreateLobbyCommand { get; }
        public ICommand JoinLobbyCommand { get; }
        public ICommand StartMatchCommand { get; }
        public ICommand LeaveLobbyCommand { get; }
        public ICommand CopyInviteLinkCommand { get; }

        private bool IsCurrentUserHost()
        {
            // 1) Si en la lista aparece mi usuario, confío en el flag IsHost
            foreach (var m in Members)
            {
                if (!string.IsNullOrWhiteSpace(m?.UserName) &&
                    string.Equals(m.UserName, CurrentUserName, StringComparison.OrdinalIgnoreCase))
                {
                    return m.IsHost;
                }

                if (m.UserId == CurrentUserId) // por si Id sí coincide
                {
                    return m.IsHost;
                }
            }

            // 2) Fallback: nombre del host que reporta el servidor
            if (!string.IsNullOrWhiteSpace(HostUserName) &&
                string.Equals(HostUserName, CurrentUserName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 3) Último recurso: comparación por Id
            return HostUserId == CurrentUserId;
        }


        public bool CanStartMatch =>
            Members.Count >= AppConstants.MIN_PLAYERS_TO_START &&
            Members.Count <= MaxPlayers &&
            IsCurrentUserHost() &&
            string.Equals(LobbyStatus, "Waiting", StringComparison.OrdinalIgnoreCase);


        public LobbyViewModel()
        {
            var sc = SessionContext.Current;
            if (sc.IsAuthenticated)
            {
                CurrentUserId = sc.UserId;
                CurrentUserName = string.IsNullOrWhiteSpace(sc.UserName) ? "Unknown" : sc.UserName.Trim();
            }
            else
            {
                var fallbackName = $"Guest-{Environment.UserName}-{Process.GetCurrentProcess().Id}";
                CurrentUserName = fallbackName;
                CurrentUserId = Math.Abs(fallbackName.GetHashCode());
            }

            CreateLobbyCommand = new AsyncCommand(CreateLobbyAsync);
            JoinLobbyCommand = new AsyncCommand(JoinLobbyAsync, () => !string.IsNullOrWhiteSpace(CodigoInput));
            StartMatchCommand = new AsyncCommand(StartMatchAsync, () => CanStartMatch);
            LeaveLobbyCommand = new AsyncCommand(LeaveLobbyAsync);
            CopyInviteLinkCommand = new RelayCommand(_ => CopyInviteLink());

            pollTimer.Tick += async (_, __) => await RefreshLobbyAsync();

            Members.CollectionChanged += OnMembersChanged;
        }

        private void OnMembersChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateStatus();
            NotifyStartAvailabilityChanged();
        }
        public void ApplyCreateOptions(CreateMatchOptions options)
        {
            if (options == null) return;

            BoardSize = options.BoardSize;
            Difficulty = options.Difficulty;
            SpecialTiles = options.SpecialTiles;
            IsPrivate = options.IsPrivate;
            RoomKey = options.RoomKey ?? string.Empty;

            var players = options.Players;
            
            PlayersRequested = (byte)players;
        }

        private async Task CreateLobbyAsync()
        {
            try
            {
                using (var client = new LobbyServiceClient(LOBBY_ENDPOINT))
                {
                    var res = await client.CreateGameAsync(new CreateGameRequest
                    {
                        HostUserId = CurrentUserId,
                        
                        MaxPlayers = PlayersRequested,                 
                        Dificultad = MapDifficulty(Difficulty),
                        TtlMinutes = AppConstants.DEFAULT_TTL_MINUTES
                    });

                    LobbyId = res.PartidaId;
                    CodigoPartida = res.CodigoPartida;
                    ExpiresAtUtc = res.ExpiresAtUtc;

                    HostUserId = CurrentUserId;
                    HostUserName = CurrentUserName;
                    LobbyStatus = "Waiting";
                    RaiseCanExecutes();

                    MaxPlayers = PlayersRequested; // ← esto evita ver “/2” en la primera pintura


                    Members.Clear();
                    Members.Add(new LobbyMemberViewModel(CurrentUserId, CurrentUserName, true, DateTime.Now));

                    StatusText = $"Lobby creado. Código {CodigoPartida}. " +
                                 $"Límite {MaxPlayers}. Expira {ExpiresAtUtc:HH:mm} UTC";
                }

                if (!pollTimer.IsEnabled) pollTimer.Start();
                await RefreshLobbyAsync(); 
            }
            catch (Exception ex)
            {
                StatusText = $"Error creando lobby: {ex.Message}";
                Logger.Error("Error al crear lobby.", ex);
            }
            finally { RaiseCanExecutes(); }
        }


        private async Task JoinLobbyAsync()
        {
            var code = (CodigoInput ?? string.Empty).Trim();
            if (code.Length == 0)
            {
                return;
            }

            try
            {
                using (var client = new LobbyServiceClient(LOBBY_ENDPOINT))
                {
                    var join = client.JoinLobby(new JoinLobbyRequest
                    {
                        CodigoPartida = code,
                        UserId = CurrentUserId,
                        UserName = CurrentUserName
                    });

                    if (!join.Success)
                    {
                        StatusText = $"No se pudo entrar: {join.FailureReason}";
                        return;
                    }

                    var info = join.Lobby;
                    LobbyId = info.PartidaId;
                    CodigoPartida = info.CodigoPartida;
                    HostUserId = info.HostUserId;
                    HostUserName = info.HostUserName;
                    MaxPlayers = info.MaxPlayers;
                    LobbyStatus = info.Status.ToString();
                    ExpiresAtUtc = info.ExpiresAtUtc;

                    Members.SynchronizeWith(info.Players,
                        match: (vm, dto) => vm.UserId == dto.UserId,
                        selector: dto => new LobbyMemberViewModel(dto.UserId, dto.UserName, dto.IsHost, dto.JoinedAtUtc),
                        update: (vm, dto) => vm.IsHost = dto.IsHost);

                    StatusText = $"Unido a {CodigoPartida}. Host: {HostUserName}. {Members.Count}/{MaxPlayers}";
                }

                if (!pollTimer.IsEnabled)
                {
                    pollTimer.Start();
                }

                await RefreshLobbyAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"Error al unirse: {ex.Message}";
                Logger.Error("Error al unirse al lobby.", ex);
            }
            finally
            {
                RaiseCanExecutes();
            }
        }

        private async Task RefreshLobbyAsync()
        {
            if (LobbyId == 0)
            {
                return;
            }

            try
            {
                using (var client = new LobbyServiceClient(LOBBY_ENDPOINT))
                {
                    var info = client.GetLobbyInfo(new GetLobbyInfoRequest { PartidaId = LobbyId });
                    if (info == null)
                    {
                        pollTimer.Stop();
                        StatusText = "El lobby se cerró o expiró.";
                        Members.Clear();
                        LobbyId = 0;
                        return;
                    }

                    HostUserId = info.HostUserId;
                    HostUserName = info.HostUserName;
                    MaxPlayers = info.MaxPlayers;
                    LobbyStatus = info.Status.ToString();
                    CodigoPartida = info.CodigoPartida;
                    ExpiresAtUtc = info.ExpiresAtUtc;

                    Members.SynchronizeWith(info.Players,
                        match: (vm, dto) => vm.UserId == dto.UserId,
                        selector: dto => new LobbyMemberViewModel(dto.UserId, dto.UserName, dto.IsHost, dto.JoinedAtUtc),
                        update: (vm, dto) => vm.IsHost = dto.IsHost);

                    StatusText = $"Lobby {CodigoPartida} — Host: {HostUserName} — {Members.Count}/{MaxPlayers} — {LobbyStatus}";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error al refrescar el lobby.", ex);
            }
            finally
            {
                RaiseCanExecutes();
            }
        }

        private void NotifyStartAvailabilityChanged()
        {
            // Para el binding IsEnabled="{Binding CanStartMatch}"
            OnPropertyChanged(nameof(CanStartMatch));

            // Para el botón que usa Command
            (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }


        private async Task StartMatchAsync()
        {
            try
            {
                using (var client = new LobbyServiceClient(LOBBY_ENDPOINT))
                {
                    var result = client.StartMatch(new StartMatchRequest
                    {
                        PartidaId = LobbyId,
                        HostUserId = CurrentUserId
                        
                    });

                    StatusText = result.Message ?? (result.Success ? "Iniciando..." : "No se pudo iniciar.");
                }

                await RefreshLobbyAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"Error al iniciar: {ex.Message}";
                Logger.Error("Error al iniciar la partida.", ex);
            }
        }

        private async Task LeaveLobbyAsync()
        {
            try
            {
                using (var client = new LobbyServiceClient(LOBBY_ENDPOINT))
                {
                    var result = client.LeaveLobby(new LeaveLobbyRequest
                    {
                        PartidaId = LobbyId,
                        UserId = CurrentUserId
                    });

                    StatusText = result.Message ?? "Saliste del lobby.";
                }

                pollTimer.Stop();
                Members.Clear();
                LobbyId = 0;
            }
            catch (Exception ex)
            {
                StatusText = $"Error al salir: {ex.Message}";
                Logger.Error("Error al salir del lobby.", ex);
            }
        }

        private void CopyInviteLink()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(CodigoPartida))
                {
                    Clipboard.SetText(CodigoPartida);
                    StatusText = "Código copiado al portapapeles.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error al copiar el código al portapapeles.", ex);
            }
        }

        private static string MapDifficulty(DifficultyOption value)
        {
            switch (value)
            {
                case DifficultyOption.Easy:
                    return "Easy";
                case DifficultyOption.Hard:
                    return "Hard";
                default:
                    return "Normal";
            }
        }

        private void RaiseCanExecutes()
        {
            (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            (JoinLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void UpdateStatus()
        {
            if (LobbyId <= 0)
            {
                StatusText = "Sin lobby";
                return;
            }

            StatusText = $"Lobby {CodigoPartida} — Host: {HostUserName} — {Members.Count}/{MaxPlayers} — {LobbyStatus}";
        }


    }



    internal static class CollectionSyncExtensions
    {
        public static void SynchronizeWith<TVm, TDto>(
            this ObservableCollection<TVm> target,
            System.Collections.Generic.IEnumerable<TDto> source,
            Func<TVm, TDto, bool> match,
            Func<TDto, TVm> selector,
            Action<TVm, TDto> update)
        {
            
            for (int i = target.Count - 1; i >= 0; i--)
            {
                var vm = target[i];
                bool exists = false;
                foreach (var dto in source)
                {
                    if (match(vm, dto))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    target.RemoveAt(i);
                }
            }

            foreach (var dto in source)
            {
                TVm found = default(TVm);
                foreach (var vm in target)
                {
                    if (match(vm, dto))
                    {
                        found = vm;
                        break;
                    }
                }

                if (Equals(found, default(TVm)))
                {
                    target.Add(selector(dto));
                }
                else
                {
                    update(found, dto);
                }
            }
        }
    }
}
