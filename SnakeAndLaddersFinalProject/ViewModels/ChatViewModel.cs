using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.ChatService;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatViewModel));
        private IChatService proxy;
        private string newMessage = string.Empty;
        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(3);

        public int LobbyId { get; }
        public ObservableCollection<ChatMessageVm> Messages { get; } = new ObservableCollection<ChatMessageVm>();
        public string NewMessage { get => newMessage; set { newMessage = value; OnPropertyChanged(nameof(NewMessage)); } }
        public bool IsAutoScrollEnabled { get; set; } = true;
        public string StatusText { get; private set; } = "Ready";
        public string CurrentUserName { get; }
        public int CurrentUserId { get; }

        public ICommand SendMessageCommand { get; }
        public ICommand CopyMessageCommand { get; }
        public ICommand QuoteMessageCommand { get; }
        public ICommand OpenStickersCommand { get; }

        public ChatViewModel(int lobbyId)
        {
            LobbyId = lobbyId;

            CurrentUserId = SessionContext.Current.UserId;
            CurrentUserName = string.IsNullOrWhiteSpace(SessionContext.Current.UserName)
                ? "Guest"
                : SessionContext.Current.UserName;

            proxy = CreateDuplexProxyFromConfig();

            try
            {
                proxy.Subscribe(LobbyId, CurrentUserId);

                var recent = proxy.GetRecent(LobbyId, 50);
                foreach (var dto in recent)
                    Messages.Add(new ChatMessageVm(dto, CurrentUserName));
            }
            catch (Exception ex)
            {
                StatusText = $"Chat offline: {ex.Message}";
                OnPropertyChanged(nameof(StatusText));
                Logger.Error("Failed to subscribe or get recent messages.", ex);
            }

            SendMessageCommand = new RelayCommand(_ => Send(), _ => CanSend());
            CopyMessageCommand = new RelayCommand(m => Copy(m as ChatMessageVm));
            QuoteMessageCommand = new RelayCommand(m => Quote(m as ChatMessageVm));
            OpenStickersCommand = new RelayCommand(_ => ShowStickers());
        }

        private IChatService CreateDuplexProxyFromConfig()
        {
            string bindingName = ConfigurationManager.AppSettings["ChatBinding"] ?? "netTcpBinding";
            string address = ConfigurationManager.AppSettings["ChatEndpointAddress"] ?? "net.tcp://localhost:8087/chat";

            var ctx = new InstanceContext(new ChatClientCallback(this));

            if (bindingName.Equals("basicHttpBinding", StringComparison.OrdinalIgnoreCase) ||
                bindingName.Equals("wsDualHttpBinding", StringComparison.OrdinalIgnoreCase))
            {
                var binding = new WSDualHttpBinding();
                var endpoint = new EndpointAddress(address);
                return new DuplexChannelFactory<IChatService>(ctx, binding, endpoint).CreateChannel();
            }

            var tcp = new NetTcpBinding(SecurityMode.None) { MaxReceivedMessageSize = 1_048_576 };
            return new DuplexChannelFactory<IChatService>(ctx, tcp, new EndpointAddress(address)).CreateChannel();
        }

        private bool CanSend() => !string.IsNullOrWhiteSpace(NewMessage);

        private void Send()
        {
            var text = (NewMessage ?? string.Empty).Trim();
            if (text.Length == 0) return;

            var localDto = new ChatMessageDto
            {
                Sender = CurrentUserName,
                SenderId = CurrentUserId,
                Text = text,
                TimestampUtc = DateTime.UtcNow
            };

            
            Messages.Add(new ChatMessageVm(localDto, CurrentUserName));

            NewMessage = string.Empty;
            OnPropertyChanged(nameof(NewMessage));

            try
            {
                var resp = proxy.SendMessage(new SendMessageRequest2
                {
                    LobbyId = LobbyId,
                    Message = localDto
                });

                if (!resp.Ok)
                {
                    StatusText = "Send failed (resp.Ok=false).";
                    OnPropertyChanged(nameof(StatusText));
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Send failed: {ex.Message}";
                OnPropertyChanged(nameof(StatusText));
                TryRecreateProxy();
                Logger.Error("Failed to send chat message.", ex);
            }
        }

        private bool IsDuplicateIncoming(ChatMessageDto dto)
        {
            if (dto == null) return false;

            var lastSameSender = Messages.LastOrDefault(
                m => string.Equals(m.Sender, dto.Sender, StringComparison.OrdinalIgnoreCase));

            if (lastSameSender == null) return false;

            var sameText = string.Equals((lastSameSender.Text ?? "").Trim(),
                                         (dto.Text ?? "").Trim(),
                                         StringComparison.Ordinal);
            if (!sameText) return false;

            var delta = (dto.TimestampUtc.ToLocalTime() - lastSameSender.SentAt);
            if (delta < TimeSpan.Zero) delta = -delta;

            return delta <= DuplicateWindow;
        }

        internal void AddIncoming(ChatMessageDto dto)
        {
            if (IsDuplicateIncoming(dto)) return;
            Messages.Add(new ChatMessageVm(dto, CurrentUserName));
        }

        private void TryRecreateProxy()
        {
            try
            {
                proxy = CreateDuplexProxyFromConfig();
                proxy.Subscribe(LobbyId, CurrentUserId);
            }
            catch (Exception ex) 
            { 
               Logger.Error("Failed to recreate chat proxy.", ex);
            }
        }

        public void Dispose()
        {
            try { proxy?.Unsubscribe(LobbyId, CurrentUserId); } catch ( Exception ex) { Logger.Error(ex); }
            if (proxy is ICommunicationObject comm)
            {
                try { comm.Close(); } catch  (Exception ex) { comm.Abort(); Logger.Error(ex); }
            }
        }

        private static void Copy(ChatMessageVm vm) { if (vm != null) Clipboard.SetText(vm.Text ?? ""); }
        private void Quote(ChatMessageVm vm) { if (vm != null) { NewMessage = $"> {vm.Text}\n" + NewMessage; OnPropertyChanged(nameof(NewMessage)); } }
        private void ShowStickers() { MessageBox.Show("Stickers pronto ✨", "Stickers", MessageBoxButton.OK, MessageBoxImage.Information); }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
