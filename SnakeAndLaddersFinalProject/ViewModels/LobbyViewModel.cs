using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using SnakeAndLaddersFinalProject.LobbyService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class LobbyViewModel : INotifyPropertyChanged
    {
        // Usa el name EXACTO del endpoint en tu App.config
        private const string LOBBY_ENDPOINT = "NetTcpBinding_ILobbyService";

        // ===== Estado/UI =====
        private readonly DispatcherTimer _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        private string _statusText = "Lobby listo.";
        private string _codigoInput = string.Empty;

        public string StatusText { get => _statusText; private set { _statusText = value; OnPropertyChanged(); } }

        // Escribe aquí el código de partida antes de pulsar "Unirse por código"
        public string CodigoInput
        {
            get => _codigoInput;
            set
            {
                if (_codigoInput == value) return;
                _codigoInput = value;
                OnPropertyChanged();
                // habilita/deshabilita el botón de Join en tiempo real
                (JoinLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<LobbyMemberViewModel> Members { get; } = new ObservableCollection<LobbyMemberViewModel>();

        // ===== Datos de lobby (contrato usa int) =====
        public int CurrentUserId { get; private set; }
        public int LobbyId { get; private set; }
        public int HostUserId { get; private set; }
        public string HostUserName { get; private set; }
        public string CodigoPartida { get; private set; }
        public byte MaxPlayers { get; private set; }
        public string LobbyStatus { get; private set; } = "Waiting";
        public DateTime ExpiresAtUtc { get; private set; }

        // ===== Comandos =====
        public ICommand CreateLobbyCommand { get; }
        public ICommand JoinLobbyCommand { get; }
        public ICommand StartMatchCommand { get; }
        public ICommand LeaveLobbyCommand { get; }
        public ICommand CopyInviteLinkCommand { get; }

        public bool CanStartMatch =>
            Members.Count >= 2 &&
            HostUserId == CurrentUserId &&
            string.Equals(LobbyStatus, "Waiting", StringComparison.OrdinalIgnoreCase);

        public LobbyViewModel()
        {
            // Identidad por instancia:
            // 1) SNL_TEST_USER si existe (lo pones al lanzar cada .exe)
            // 2) Si no, cae a "UserName-PID" para que cada proceso sea único
            var nameEnv = Environment.GetEnvironmentVariable("SNL_TEST_USER");
            var fallback = $"{Environment.UserName}-{Process.GetCurrentProcess().Id}";
            var displayName = string.IsNullOrWhiteSpace(nameEnv) ? fallback : nameEnv.Trim();

            CurrentUserId = Math.Abs(displayName.GetHashCode()); // int, estable por instancia
            _displayName = displayName;

            CreateLobbyCommand = new AsyncCommand(CreateLobbyAsync);
            JoinLobbyCommand = new AsyncCommand(JoinLobbyAsync, () => !string.IsNullOrWhiteSpace(CodigoInput));
            StartMatchCommand = new AsyncCommand(StartMatchAsync, () => CanStartMatch);
            LeaveLobbyCommand = new AsyncCommand(LeaveLobbyAsync);
            CopyInviteLinkCommand = new RelayCommand(CopyInviteLink);

            _pollTimer.Tick += async (_, __) => await RefreshLobbyAsync();
        }

        // guardamos el nombre a nivel VM
        private readonly string _displayName;

        // ===== Flujo host: crear lobby =====
        private async Task CreateLobbyAsync()
        {
            try
            {
                using (var client = new LobbyServiceClient(LOBBY_ENDPOINT))
                {
                    var res = await client.CreateGameAsync(new CreateGameRequest
                    {
                        HostUserId = CurrentUserId,
                        MaxPlayers = 4,
                        Dificultad = "Normal",
                        TtlMinutes = 30
                    });

                    LobbyId = res.PartidaId;
                    CodigoPartida = res.CodigoPartida;
                    ExpiresAtUtc = res.ExpiresAtUtc;

                    Members.Clear();
                    Members.Add(new LobbyMemberViewModel(CurrentUserId, _displayName, true, DateTime.Now));

                    StatusText = $"Lobby creado. Código {CodigoPartida}. Expira {ExpiresAtUtc:HH:mm} UTC";
                }

                if (!_pollTimer.IsEnabled) _pollTimer.Start();
                await RefreshLobbyAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"Error creando lobby: {ex.Message}";
            }
            finally { RaiseCanExecutes(); }
        }

        // ===== Flujo invitado: unirse por código =====
        private async Task JoinLobbyAsync()
        {
            var code = (CodigoInput ?? string.Empty).Trim();
            if (code.Length == 0) return;

            try
            {
                using (var client = new LobbyServiceClient(LOBBY_ENDPOINT))
                {
                    var join = client.JoinLobby(new JoinLobbyRequest
                    {
                        CodigoPartida = code,
                        UserId = CurrentUserId,
                        UserName = _displayName
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

                if (!_pollTimer.IsEnabled) _pollTimer.Start();
                await RefreshLobbyAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"Error al unirse: {ex.Message}";
            }
            finally { RaiseCanExecutes(); }
        }

        // ===== Polling de estado =====
        private async Task RefreshLobbyAsync()
        {
            if (LobbyId == 0) return;

            try
            {
                using (var client = new LobbyServiceClient(LOBBY_ENDPOINT))
                {
                    var info = client.GetLobbyInfo(new GetLobbyInfoRequest { PartidaId = LobbyId });
                    if (info == null)
                    {
                        _pollTimer.Stop();
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
            catch
            {
                // fallas transitorias: reintenta en el siguiente tick
            }
            finally { RaiseCanExecutes(); }
        }

        // ===== Acciones =====
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
                _pollTimer.Stop();
                Members.Clear();
                LobbyId = 0;
            }
            catch (Exception ex)
            {
                StatusText = $"Error al salir: {ex.Message}";
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
            catch { /* no-op */ }
        }

        private void RaiseCanExecutes()
        {
            (StartMatchCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            (JoinLobbyCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Helper para sincronizar la colección sin romper bindings
    internal static class CollectionSyncExtensions
    {
        public static void SynchronizeWith<TVm, TDto>(
            this ObservableCollection<TVm> target,
            System.Collections.Generic.IEnumerable<TDto> source,
            Func<TVm, TDto, bool> match,
            Func<TDto, TVm> selector,
            Action<TVm, TDto> update)
        {
            // remove
            for (int i = target.Count - 1; i >= 0; i--)
            {
                var vm = target[i];
                bool exists = false;
                foreach (var dto in source)
                {
                    if (match(vm, dto)) { exists = true; break; }
                }
                if (!exists) target.RemoveAt(i);
            }
            // add/update
            foreach (var dto in source)
            {
                TVm found = default(TVm);
                foreach (var vm in target)
                {
                    if (match(vm, dto)) { found = vm; break; }
                }
                if (Equals(found, default(TVm)))
                    target.Add(selector(dto));
                else
                    update(found, dto);
            }
        }
    }
}
