using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using log4net;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.ChatService;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatViewModel));

        private const string CHAT_BINDING_KEY = "ChatBinding";
        private const string CHAT_ENDPOINT_ADDRESS_KEY = "ChatEndpointAddress";

        private const string DEFAULT_CHAT_BINDING = "netTcpBinding";
        private const string DEFAULT_CHAT_ENDPOINT_ADDRESS = "net.tcp://localhost:8087/chat";

        private const string DEFAULT_STATUS_READY = "Ready";
        private const string DEFAULT_GUEST_USER_NAME = "Guest";

        private const int DEFAULT_RECENT_MESSAGES_TAKE = 50;
        private const int MAX_RECEIVED_MESSAGE_SIZE_BYTES = 1_048_576;
        private const int MAX_MESSAGE_LENGTH = 500;

        private const int GUEST_RANDOM_MIN = 10;
        private const int GUEST_RANDOM_MAX = 99;

        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(3);

        private IChatService chatServiceProxy;
        private string newMessageText = string.Empty;

        public int LobbyId { get; }

        public ObservableCollection<ChatMessageViewModel> Messages { get; } =
            new ObservableCollection<ChatMessageViewModel>();

        public string NewMessage
        {
            get => newMessageText;
            set
            {
                string safeText = value ?? string.Empty;

                if (safeText.Length > MAX_MESSAGE_LENGTH)
                {
                    safeText = safeText.Substring(0, MAX_MESSAGE_LENGTH);
                }

                newMessageText = safeText;
                OnPropertyChanged(nameof(NewMessage));
            }
        }

        public bool IsAutoScrollEnabled { get; set; } = true;

        public string StatusText { get; private set; } = DEFAULT_STATUS_READY;

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
            CurrentUserName = GetSafeUserName();

            chatServiceProxy = CreateDuplexProxyFromConfig();

            SendMessageCommand = new RelayCommand(_ => Send(), _ => CanSend());
            CopyMessageCommand = new RelayCommand(message => Copy(message as ChatMessageViewModel));
            QuoteMessageCommand = new RelayCommand(message => Quote(message as ChatMessageViewModel));
            OpenStickersCommand = new RelayCommand(_ => ShowStickers());
        }

        public async Task InitializeAsync()
        {
            try
            {
                IList<ChatMessageDto> recentMessages = await SubscribeAndLoadRecentAsync();

                foreach (ChatMessageDto messageDto in recentMessages)
                {
                    Messages.Add(new ChatMessageViewModel(messageDto, CurrentUserName));
                }

                SetStatus(DEFAULT_STATUS_READY);
            }
            catch (Exception ex)
            {
                string uiMessage = ExceptionHandler.Handle(ex, "Chat.Initialize", Logger);
                SetStatus(uiMessage);
            }
        }

        private async Task<IList<ChatMessageDto>> SubscribeAndLoadRecentAsync()
        {
            var emptyList = new List<ChatMessageDto>();

            if (chatServiceProxy == null)
            {
                return emptyList;
            }

            List<ChatMessageDto> recentMessages = emptyList;

            await Task.Run(
                () =>
                {
                    chatServiceProxy.Subscribe(LobbyId, CurrentUserId);

                    ChatMessageDto[] serviceMessages =
                        chatServiceProxy.GetRecent(LobbyId, DEFAULT_RECENT_MESSAGES_TAKE)
                        ?? Array.Empty<ChatMessageDto>();

                    if (serviceMessages.Length == 0)
                    {
                        recentMessages = emptyList;
                    }
                    else
                    {
                        recentMessages = new List<ChatMessageDto>(serviceMessages);
                    }
                });

            return recentMessages;
        }

        private bool CanSend()
        {
            return !string.IsNullOrWhiteSpace(NewMessage);
        }

        private async void Send()
        {
            string messageText = (NewMessage ?? string.Empty).Trim();

            if (messageText.Length == 0)
            {
                return;
            }

            ChatMessageDto localMessageDto = BuildLocalMessageDto(messageText);

            Messages.Add(new ChatMessageViewModel(localMessageDto, CurrentUserName));

            NewMessage = string.Empty;

            if (chatServiceProxy == null)
            {
                SetStatus(Globalization.LocalizationManager.Current["UiServiceError"]);
                return;
            }

            try
            {
                SendMessageResponse2 response = await Task.Run(
                    () => chatServiceProxy.SendMessage(
                        new SendMessageRequest2
                        {
                            LobbyId = LobbyId,
                            Message = localMessageDto
                        }));

                if (!response.Ok)
                {
                    SetStatus(Globalization.LocalizationManager.Current["UiServiceError"]);
                }
            }
            catch (Exception ex)
            {
                string uiMessage = ExceptionHandler.Handle(ex, "Chat.SendMessage", Logger);
                SetStatus(uiMessage);
                TryRecreateProxy();
            }
        }

        private ChatMessageDto BuildLocalMessageDto(string messageText)
        {
            string safeText = messageText;
            if (safeText.Length > MAX_MESSAGE_LENGTH)
            {
                safeText = safeText.Substring(0, MAX_MESSAGE_LENGTH);
            }

            var dto = new ChatMessageDto
            {
                Sender = CurrentUserName,
                SenderId = CurrentUserId,
                Text = safeText,
                TimestampUtc = DateTime.UtcNow,
                SenderAvatarId = SessionContext.Current.ProfilePhotoId ?? string.Empty
            };

            return dto;
        }

        internal void AddIncoming(ChatMessageDto messageDto)
        {
            if (IsDuplicateIncoming(messageDto))
            {
                return;
            }

            var messageViewModel = new ChatMessageViewModel(messageDto, CurrentUserName);
            Messages.Add(messageViewModel);
        }

        private bool IsDuplicateIncoming(ChatMessageDto messageDto)
        {
            if (messageDto == null)
            {
                return false;
            }

            ChatMessageViewModel lastSameSender = Messages.LastOrDefault(
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

            return delta <= DuplicateWindow;
        }

        private IChatService CreateDuplexProxyFromConfig()
        {
            string bindingName = ConfigurationManager.AppSettings[CHAT_BINDING_KEY] ?? DEFAULT_CHAT_BINDING;
            string address = ConfigurationManager.AppSettings[CHAT_ENDPOINT_ADDRESS_KEY] ?? DEFAULT_CHAT_ENDPOINT_ADDRESS;

            var instanceContext = new InstanceContext(new ChatClientCallback(this));

            if (IsHttpBinding(bindingName))
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

        private static bool IsHttpBinding(string bindingName)
        {
            if (string.IsNullOrWhiteSpace(bindingName))
            {
                return false;
            }

            string normalized = bindingName.Trim();

            if (normalized.Equals("basicHttpBinding", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (normalized.Equals("wsDualHttpBinding", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private void TryRecreateProxy()
        {
            try
            {
                chatServiceProxy = CreateDuplexProxyFromConfig();

                Task.Run(
                    () =>
                    {
                        chatServiceProxy.Subscribe(LobbyId, CurrentUserId);
                    });
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "Chat.RecreateProxy", Logger);
            }
        }

        private static void Copy(ChatMessageViewModel chatMessageViewModel)
        {
            if (chatMessageViewModel == null)
            {
                return;
            }

            string textToCopy = chatMessageViewModel.Text ?? string.Empty;
            Clipboard.SetText(textToCopy);
        }

        private void Quote(ChatMessageViewModel chatMessageViewModel)
        {
            if (chatMessageViewModel == null)
            {
                return;
            }

            string originalText = chatMessageViewModel.Text ?? string.Empty;
            string quoted = string.Format(
                "> {0}{1}{2}",
                originalText,
                Environment.NewLine,
                NewMessage ?? string.Empty);

            NewMessage = quoted;
        }

        private static void ShowStickers()
        {
            MessageBox.Show(
                "Stickers pronto ✨",
                "Stickers",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private string GetSafeUserName()
        {
            string userName = SessionContext.Current.UserName;

            if (string.IsNullOrWhiteSpace(userName))
            {
                var random = new Random();
                int randomSuffix = random.Next(GUEST_RANDOM_MIN, GUEST_RANDOM_MAX + 1);

                string guestName = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}",
                    DEFAULT_GUEST_USER_NAME,
                    randomSuffix);

                return guestName;
            }

            return userName;
        }

        private void SetStatus(string status)
        {
            StatusText = status ?? string.Empty;
            OnPropertyChanged(nameof(StatusText));
        }

        public void Dispose()
        {
            try
            {
                if (chatServiceProxy != null)
                {
                    chatServiceProxy.Unsubscribe(LobbyId, CurrentUserId);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "Chat.Dispose.Unsubscribe", Logger);
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
                    ExceptionHandler.Handle(ex, "Chat.Dispose.CloseProxy", Logger);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
