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
using SnakeAndLaddersFinalProject.Models;
using SnakeAndLaddersFinalProject.ShopService;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChatViewModel));

        private const string CHAT_BINDING_KEY = "ChatBinding";
        private const string CHAT_ENDPOINT_ADDRESS_KEY = "ChatEndpointAddress";

        private const string DEFAULT_CHAT_BINDING = "netTcpBinding";
        private const string DEFAULT_CHAT_ENDPOINT_ADDRESS = "net.tcp://localhost:8087/chat";

        private const string DEFAULT_STATUS_READY = "Ready";
        private const string DEFAULT_GUEST_USER_NAME = "Guest";

        private const int DEFAULT_RECENT_MESSAGES_TAKE = 50;
        private const int MAX_RECEIVED_MESSAGE_SIZE_BYTES = 1_048_576;
        private const int MAX_MESSAGE_LENGTH = 300; 
        private const int GUEST_RANDOM_MIN = 10;
        private const int GUEST_RANDOM_MAX = 99;

        private const int MIN_CHAT_RECENT_MESSAGES = 1;
        private const int NO_STICKER_ID = 0;

        private const string SHOP_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IShopService";
        private const string STICKER_ASSET_BASE_PATH = "pack://application:,,,/Assets/Images/Stickers/";
        private const string STICKER_ASSET_EXTENSION = ".png";

        private const string LOG_CONTEXT_BUILD_TEXT = "Chat.BuildLocalMessageDto";
        private const string LOG_CONTEXT_BUILD_STICKER = "Chat.BuildStickerMessageDto";
        private const string LOG_CONTEXT_SEND_TEXT = "Chat.SendText";
        private const string LOG_CONTEXT_SEND_STICKER = "Chat.SendSticker";
        private const string LOG_CONTEXT_INCOMING = "Chat.AddIncoming";

        private static readonly TimeSpan _duplicateWindow = TimeSpan.FromSeconds(3);

        private IChatService _chatServiceProxy;
        private readonly ObservableCollection<StickerModel> _stickers
            = new ObservableCollection<StickerModel>();
        private bool _areStickersLoaded;
        private string _newMessageText = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public int LobbyId { get; }
        public ObservableCollection<ChatMessageViewModel> Messages { get; }
            = new ObservableCollection<ChatMessageViewModel>();
        public ObservableCollection<StickerModel> Stickers => _stickers;

        public string NewMessage
        {
            get => _newMessageText;
            set
            {
                string safeText = value ?? string.Empty;
                if (safeText.Length > MAX_MESSAGE_LENGTH)
                {
                    safeText = safeText.Substring(0, MAX_MESSAGE_LENGTH);
                }
                _newMessageText = safeText;
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
            if (lobbyId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lobbyId));
            }

            LobbyId = lobbyId;
            CurrentUserId = SessionContext.Current.UserId;
            CurrentUserName = GetSafeUserName();

            _chatServiceProxy = CreateDuplexProxyFromConfig();

            SendMessageCommand = new RelayCommand(_ => Send(), _ => CanSend());
            CopyMessageCommand = new RelayCommand(message => Copy(message as ChatMessageViewModel));
            QuoteMessageCommand = new RelayCommand(message => Quote(message as ChatMessageViewModel));
            OpenStickersCommand = new RelayCommand(_ => OpenStickers());
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
                string uiMessage = ExceptionHandler.Handle(ex, "Chat.Initialize", _logger);
                SetStatus(uiMessage);
            }
        }

        private async Task<IList<ChatMessageDto>> SubscribeAndLoadRecentAsync()
        {
            var emptyList = new List<ChatMessageDto>();
            if (_chatServiceProxy == null)
            {
                return emptyList;
            }

            List<ChatMessageDto> recentMessages = emptyList;

            await Task.Run(() =>
            {
                _chatServiceProxy.Subscribe(LobbyId, CurrentUserId);
                ChatMessageDto[] serviceMessages = _chatServiceProxy.GetRecent(LobbyId,
                    DEFAULT_RECENT_MESSAGES_TAKE) ?? Array.Empty<ChatMessageDto>();

                if (serviceMessages.Length < MIN_CHAT_RECENT_MESSAGES)
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

            if (messageText.Length > MAX_MESSAGE_LENGTH)
            {
                MessageBox.Show(
                    string.Format(Globalization.LocalizationManager.Current["ChatTextTooLongFmt"],
                    MAX_MESSAGE_LENGTH),
                    Globalization.LocalizationManager.Current["UiTitleWarning"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ChatMessageDto localMessageDto = BuildLocalMessageDto(messageText);

            _logger.InfoFormat("{0}. Sender={1}, Text='{2}', StickerId={3}, StickerCode='{4}'",
                LOG_CONTEXT_SEND_TEXT, localMessageDto.Sender, localMessageDto.Text, localMessageDto.StickerId,
                localMessageDto.StickerCode);

            Messages.Add(new ChatMessageViewModel(localMessageDto, CurrentUserName));
            NewMessage = string.Empty;

            if (_chatServiceProxy == null)
            {
                SetStatus(Globalization.LocalizationManager.Current["UiServiceError"]);
                return;
            }

            try
            {
                SendMessageResponse2 response = await Task.Run(() => _chatServiceProxy.SendMessage(
                    new SendMessageRequest2 { LobbyId = LobbyId, Message = localMessageDto }));
                if (!response.Ok)
                {
                    SetStatus(Globalization.LocalizationManager.Current["UiServiceError"]);
                }
            }
            catch (Exception ex)
            {
                string uiMessage = ExceptionHandler.Handle(ex, "Chat.SendMessage", _logger);
                SetStatus(uiMessage);
                TryRecreateProxy();
            }
        }

        private ChatMessageDto BuildLocalMessageDto(string messageText)
        {
            string safeText = messageText ?? string.Empty;
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
                SenderAvatarId = SessionContext.Current.ProfilePhotoId ?? string.Empty,
                StickerId = NO_STICKER_ID,
                StickerCode = string.Empty
            };

            _logger.InfoFormat("{0}. Sender={1}, Text='{2}', StickerId={3}, StickerCode='{4}'",
                LOG_CONTEXT_BUILD_TEXT, dto.Sender, dto.Text, dto.StickerId, dto.StickerCode);

            return dto;
        }

        internal void AddIncoming(ChatMessageDto messageDto)
        {
            if (messageDto == null)
            {
                return;
            }

            _logger.InfoFormat("{0}. Incoming. Sender={1}, Text='{2}', StickerId={3}, StickerCode='{4}'",
                LOG_CONTEXT_INCOMING, messageDto.Sender, messageDto.Text, messageDto.StickerId,
                messageDto.StickerCode);

            if (IsDuplicateIncoming(messageDto))
            {
                return;
            }

            Messages.Add(new ChatMessageViewModel(messageDto, CurrentUserName));
        }

        private bool IsDuplicateIncoming(ChatMessageDto messageDto)
        {
            var lastSameSender = Messages.LastOrDefault(message => string.Equals(message.Sender,
                messageDto.Sender, StringComparison.OrdinalIgnoreCase));

            if (lastSameSender == null)
            {
                return false;
            }

            bool isSameText = string.Equals((lastSameSender.Text ?? string.Empty).Trim(),
                (messageDto.Text ?? string.Empty).Trim(), StringComparison.Ordinal);

            if (!isSameText)
            {
                return false;
            }

            TimeSpan delta = messageDto.TimestampUtc.ToLocalTime() - lastSameSender.SentAt;
            if (delta < TimeSpan.Zero)
            {
                delta = -delta;
            }

            return delta <= _duplicateWindow;
        }

        private IChatService CreateDuplexProxyFromConfig()
        {
            string bindingName = ConfigurationManager.AppSettings[CHAT_BINDING_KEY] ?? DEFAULT_CHAT_BINDING;
            string address = ConfigurationManager.AppSettings[CHAT_ENDPOINT_ADDRESS_KEY]
                ?? DEFAULT_CHAT_ENDPOINT_ADDRESS;
            var instanceContext = new InstanceContext(new ChatClientCallback(this));

            if (IsHttpBinding(bindingName))
            {
                var wsBinding = new WSDualHttpBinding();
                var wsEndpoint = new EndpointAddress(address);
                return new DuplexChannelFactory<IChatService>(instanceContext, wsBinding, wsEndpoint)
                    .CreateChannel();
            }

            var netTcpBinding = new NetTcpBinding(SecurityMode.None)
            {
                MaxReceivedMessageSize = MAX_RECEIVED_MESSAGE_SIZE_BYTES
            };
            var endpoint = new EndpointAddress(address);
            return new DuplexChannelFactory<IChatService>(instanceContext, netTcpBinding, endpoint)
                .CreateChannel();
        }

        private static bool IsHttpBinding(string bindingName)
        {
            if (string.IsNullOrWhiteSpace(bindingName))
            {
                return false;
            }
            string normalized = bindingName.Trim();
            return normalized.Equals("basicHttpBinding", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("wsDualHttpBinding", StringComparison.OrdinalIgnoreCase);
        }

        private void TryRecreateProxy()
        {
            try
            {
                IChatService newProxy = CreateDuplexProxyFromConfig();
                Task.Run(() => newProxy.Subscribe(LobbyId, CurrentUserId));
                _chatServiceProxy = newProxy;
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "Chat.RecreateProxy", _logger);
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
            string quoted = string.Format("> {0}{1}{2}", originalText, Environment.NewLine, NewMessage ??
                string.Empty);
            NewMessage = quoted;
        }

        private string GetSafeUserName()
        {
            string userName = SessionContext.Current.UserName;
            if (string.IsNullOrWhiteSpace(userName))
            {
                Random random = new Random();
                int randomSuffix = random.Next(GUEST_RANDOM_MIN, GUEST_RANDOM_MAX + 1);
                return string.Format(CultureInfo.InvariantCulture, "{0}{1}", DEFAULT_GUEST_USER_NAME,
                    randomSuffix);
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
                if (_chatServiceProxy != null)
                {
                    _chatServiceProxy.Unsubscribe(LobbyId, CurrentUserId);
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "Chat.Dispose.Unsubscribe", _logger);
            }

            if (_chatServiceProxy is ICommunicationObject communicationObject)
            {
                try
                {
                    communicationObject.Close();
                }
                catch (Exception ex)
                {
                    communicationObject.Abort();
                    ExceptionHandler.Handle(ex, "Chat.Dispose.CloseProxy", _logger);
                }
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenStickers()
        {
            if (!IsUserAuthenticatedForStickers())
            {
                return;
            }
            ShowStickerPickerAsync();
        }

        private async void ShowStickerPickerAsync()
        {
            try
            {
                if (!_areStickersLoaded)
                {
                    await LoadStickersAsync();
                }

                if (Stickers.Count == 0)
                {
                    MessageBox.Show(Globalization.LocalizationManager.Current["ChatNoStickersOwned"],
                        Globalization.LocalizationManager.Current["UiTitleInfo"], MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var pickerWindow = new Windows.StickerPickerWindow(Stickers)
                {
                    Owner = Application.Current.MainWindow
                };

                bool? dialogResult = pickerWindow.ShowDialog();
                if (dialogResult == true && pickerWindow.SelectedSticker != null)
                {
                    SendSticker(pickerWindow.SelectedSticker);
                }
            }
            catch (Exception ex)
            {
                string uiMessage = ExceptionHandler.Handle(ex, "Chat.ShowStickerPickerAsync", _logger);
                SetStatus(uiMessage);
            }
        }

        private async Task LoadStickersAsync()
        {
            Stickers.Clear();
            _areStickersLoaded = false;

            if (!IsUserAuthenticatedForStickers())
            {
                return;
            }

            var shopServiceClient = new ShopServiceClient(SHOP_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                string token = SessionContext.Current.AuthToken ?? string.Empty;
                StickerDto[] serviceStickers = await Task.Run(() => shopServiceClient.GetUserStickers(token))
                    .ConfigureAwait(true);
                StickerDto[] safeStickers = serviceStickers ?? Array.Empty<StickerDto>();

                foreach (StickerDto dto in safeStickers)
                {
                    string imagePath = BuildStickerAssetPath(dto.StickerCode);
                    var sticker = new StickerModel(dto.StickerId, dto.StickerCode, dto.StickerName, imagePath);
                    Stickers.Add(sticker);
                }
                _areStickersLoaded = true;
            }
            catch (FaultException faultException)
            {
                MessageBox.Show(faultException.Message, Globalization.LocalizationManager.Current["UiTitleError"],
                    MessageBoxButton.OK, MessageBoxImage.Error);
                shopServiceClient.Abort();
            }
            catch (EndpointNotFoundException)
            {
                MessageBox.Show(Globalization.LocalizationManager.Current["UiEndpointNotFound"],
                    Globalization.LocalizationManager.Current["UiTitleError"], MessageBoxButton.OK,
                    MessageBoxImage.Error);
                shopServiceClient.Abort();
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, "Chat.LoadStickersAsync", _logger);
                shopServiceClient.Abort();
            }
            finally
            {
                if (shopServiceClient.State == CommunicationState.Faulted)
                {
                    shopServiceClient.Abort();
                }
                else
                {
                    shopServiceClient.Close();
                }
            }
        }

        private void SendSticker(StickerModel sticker)
        {
            if (sticker == null)
            {
                return;
            }

            ChatMessageDto localMessageDto = BuildStickerMessageDto(sticker.StickerId, sticker.StickerCode);
            _logger.InfoFormat("{0}. Sender={1}, Text='{2}', StickerId={3}, StickerCode='{4}'",
                LOG_CONTEXT_SEND_STICKER, localMessageDto.Sender, localMessageDto.Text, localMessageDto.StickerId, localMessageDto.StickerCode);

            Messages.Add(new ChatMessageViewModel(localMessageDto, CurrentUserName));

            if (_chatServiceProxy == null)
            {
                SetStatus(Globalization.LocalizationManager.Current["UiServiceError"]);
                return;
            }

            try
            {
                Task.Run(() => _chatServiceProxy.SendMessage(new SendMessageRequest2 { LobbyId = LobbyId, Message = localMessageDto }));
            }
            catch (Exception ex)
            {
                string uiMessage = ExceptionHandler.Handle(ex, "Chat.SendSticker", _logger);
                SetStatus(uiMessage);
                TryRecreateProxy();
            }
        }

        private ChatMessageDto BuildStickerMessageDto(int stickerId, string stickerCode)
        {
            var stickerMessageDto = new ChatMessageDto
            {
                Sender = CurrentUserName,
                SenderId = CurrentUserId,
                Text = string.Empty,
                TimestampUtc = DateTime.UtcNow,
                SenderAvatarId = SessionContext.Current.ProfilePhotoId ?? string.Empty,
                StickerId = stickerId,
                StickerCode = stickerCode ?? string.Empty
            };

            _logger.InfoFormat("{0}. Sender={1}, Text='{2}', StickerId={3}, StickerCode='{4}'",
                LOG_CONTEXT_BUILD_STICKER, stickerMessageDto.Sender, stickerMessageDto.Text, stickerMessageDto.StickerId, stickerMessageDto.StickerCode);

            return stickerMessageDto;
        }

        private bool IsUserAuthenticatedForStickers()
        {
            if (SessionContext.Current == null || !SessionContext.Current.IsAuthenticated)
            {
                MessageBox.Show(Globalization.LocalizationManager.Current["UiShopRequiresLogin"], Globalization.LocalizationManager.Current["UiTitleWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        internal static string BuildStickerAssetPath(string stickerCode)
        {
            if (string.IsNullOrWhiteSpace(stickerCode))
            {
                return string.Empty;
            }
            return string.Concat(STICKER_ASSET_BASE_PATH, stickerCode, STICKER_ASSET_EXTENSION);
        }
    }
}
