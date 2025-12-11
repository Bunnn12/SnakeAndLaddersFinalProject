using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public sealed class ChatClientCallback : IChatServiceCallback
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChatClientCallback));

        private const string LOG_CONTEXT_CALLBACK = "ChatClientCallback.OnMessage";

        private readonly ChatViewModel _chatViewModel;
        private readonly Dispatcher _uiDispatcher;

        public ChatClientCallback(ChatViewModel chatViewModel)
        {
            _chatViewModel = chatViewModel ?? throw new ArgumentNullException(nameof(chatViewModel));
            _uiDispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public void OnMessage(int lobbyId, ChatMessageDto message)
        {
            if (message == null)
            {
                return;
            }

            _logger.InfoFormat(
                "{0}. LobbyId={1}, Sender={2}, Text='{3}', StickerId={4}, StickerCode='{5}'",
                LOG_CONTEXT_CALLBACK,
                lobbyId,
                message.Sender,
                message.Text,
                message.StickerId,
                message.StickerCode);

            _uiDispatcher.BeginInvoke(new Action(() => _chatViewModel.AddIncoming(message)));
        }
    }
}
