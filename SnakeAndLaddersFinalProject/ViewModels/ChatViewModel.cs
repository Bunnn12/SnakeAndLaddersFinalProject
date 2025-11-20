using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatViewModel));

        private const string CHAT_BINDING_KEY = "ChatBinding";
        private const string CHAT_ENDPOINT_ADDRESS_KEY = "ChatEndpointAddress";

        private const string DEFAULT_CHAT_BINDING = "netTcpBinding";
        private const string DEFAULT_CHAT_ENDPOINT_ADDRESS = "net.tcp://localhost:8087/chat";
        private const string STATUS_READY_TEXT = "Ready";
        private const string DEFAULT_GUEST_USER_NAME = "Guest";

        private const int DEFAULT_RECENT_MESSAGES_TAKE = 50;
        private const int MAX_RECEIVED_MESSAGE_SIZE_BYTES = 1_048_576;

        private static readonly TimeSpan DUPLICATE_WINDOW = TimeSpan.FromSeconds(3);

        private IChatService chatServiceProxy;
        private string newMessageText = string.Empty;

        public int LobbyId { get; }

        public ObservableCollection<ChatMessageVm> Messages { get; } = new ObservableCollection<ChatMessageVm>();

        public string NewMessage
        {
            get => newMessageText;
            set
            {
                newMessageText = value;
                OnPropertyChanged(nameof(NewMessage));
            }
        }

        public bool IsAutoScrollEnabled { get; set; } = true;

        public string StatusText { get; private set; } = STATUS_READY_TEXT;

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
                ? DEFAULT_GUEST_USER_NAME
                : SessionContext.Current.UserName;

            chatServiceProxy = CreateDuplexProxyFromConfig();

            try
            {
                chatServiceProxy.Subscribe(LobbyId, CurrentUserId);

                var recentMessages = chatServiceProxy.GetRecent(LobbyId, DEFAULT_RECENT_MESSAGES_TAKE);
                foreach (var message in recentMessages)
                {
                    Messages.Add(new ChatMessageVm(message, CurrentUserName));
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Chat offline: {ex.Message}";
                OnPropertyChanged(nameof(StatusText));
                Logger.Error("Failed to subscribe or get recent messages.", ex);
            }

            SendMessageCommand = new RelayCommand(_ => Send(), _ => CanSend());
            CopyMessageCommand = new RelayCommand(message => Copy(message as ChatMessageVm));
            QuoteMessageCommand = new RelayCommand(message => Quote(message as ChatMessageVm));
            OpenStickersCommand = new RelayCommand(_ => ShowStickers());
        }

        private IChatService CreateDuplexProxyFromConfig()
        {
            string bindingName = ConfigurationManager.AppSettings[CHAT_BINDING_KEY] ?? DEFAULT_CHAT_BINDING;
            string address = ConfigurationManager.AppSettings[CHAT_ENDPOINT_ADDRESS_KEY] ?? DEFAULT_CHAT_ENDPOINT_ADDRESS;

            var instanceContext = new InstanceContext(new ChatClientCallback(this));

            if (bindingName.Equals("basicHttpBinding", StringComparison.OrdinalIgnoreCase) ||
                bindingName.Equals("wsDualHttpBinding", StringComparison.OrdinalIgnoreCase))
            {
                var wsBinding = new WSDualHttpBinding();
                var wsEndpoint = new EndpointAddress(address);
                return new DuplexChannelFactory<IChatService>(instanceContext, wsBinding, wsEndpoint).CreateChannel();
            }

            var netTcpBinding = new NetTcpBinding(SecurityMode.None)
            {
                MaxReceivedMessageSize = MAX_RECEIVED_MESSAGE_SIZE_BYTES
            };

            var endpoint = new EndpointAddress(address);
            return new DuplexChannelFactory<IChatService>(instanceContext, netTcpBinding, endpoint).CreateChannel();
        }

        private bool CanSend()
        {
            return !string.IsNullOrWhiteSpace(NewMessage);
        }

        private void Send()
        {
            string messageText = (NewMessage ?? string.Empty).Trim();
            if (messageText.Length == 0)
            {
                return;
            }

            var localMessageDto = new ChatMessageDto
            {
                Sender = CurrentUserName,
                SenderId = CurrentUserId,
                Text = messageText,
                TimestampUtc = DateTime.UtcNow,
                SenderAvatarId = SessionContext.Current.ProfilePhotoId
            };

            Messages.Add(new ChatMessageVm(localMessageDto, CurrentUserName));

            NewMessage = string.Empty;
            OnPropertyChanged(nameof(NewMessage));

            try
            {
                var response = chatServiceProxy.SendMessage(
                    new SendMessageRequest2
                    {
                        LobbyId = LobbyId,
                        Message = localMessageDto
                    });

                if (!response.Ok)
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

        private bool IsDuplicateIncoming(ChatMessageDto messageDto)
        {
            if (messageDto == null)
            {
                return false;
            }

            var lastSameSender = Messages.LastOrDefault(
                message => string.Equals(
                    message.Sender,
                    messageDto.Sender,
                    StringComparison.OrdinalIgnoreCase));

            if (lastSameSender == null)
            {
                return false;
            }

            bool isSameText = string.Equals(
                (lastSameSender.Text ?? string.Empty).Trim(),
                (messageDto.Text ?? string.Empty).Trim(),
                StringComparison.Ordinal);

            if (!isSameText)
            {
                return false;
            }

            TimeSpan delta = messageDto.TimestampUtc.ToLocalTime() - lastSameSender.SentAt;
            if (delta < TimeSpan.Zero)
            {
                delta = -delta;
            }

            return delta <= DUPLICATE_WINDOW;
        }

        internal void AddIncoming(ChatMessageDto messageDto)
        {
            if (IsDuplicateIncoming(messageDto))
            {
                return;
            }

            Messages.Add(new ChatMessageVm(messageDto, CurrentUserName));
        }

        private void TryRecreateProxy()
        {
            try
            {
                chatServiceProxy = CreateDuplexProxyFromConfig();
                chatServiceProxy.Subscribe(LobbyId, CurrentUserId);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to recreate chat proxy.", ex);
            }
        }

        public void Dispose()
        {
            try
            {
                chatServiceProxy?.Unsubscribe(LobbyId, CurrentUserId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            if (chatServiceProxy is ICommunicationObject communicationObject)
            {
                try
                {
                    communicationObject.Close();
                }
                catch (Exception ex)
                {
                    communicationObject.Abort();
                    Logger.Error(ex);
                }
            }
        }

        private static void Copy(ChatMessageVm chatMessageViewModel)
        {
            if (chatMessageViewModel == null)
            {
                return;
            }

            Clipboard.SetText(chatMessageViewModel.Text ?? string.Empty);
        }

        private void Quote(ChatMessageVm chatMessageViewModel)
        {
            if (chatMessageViewModel == null)
            {
                return;
            }

            NewMessage = $"> {chatMessageViewModel.Text}\n{NewMessage}";
            OnPropertyChanged(nameof(NewMessage));
        }

        private static void ShowStickers()
        {
            MessageBox.Show(
                "Stickers pronto ✨",
                "Stickers",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
