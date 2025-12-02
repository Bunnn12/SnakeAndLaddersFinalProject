using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Threading;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public sealed class ChatClientCallback : IChatServiceCallback
    {
        private readonly ChatViewModel _chatViewModel;
        private readonly Dispatcher _uiDispatcher;

        public ChatClientCallback(ChatViewModel chatViewModelValue)
        {
            _chatViewModel = chatViewModelValue ?? throw new ArgumentNullException(nameof(chatViewModelValue));
            _uiDispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public void OnMessage(int lobbyId, ChatMessageDto message)
        {
            if (message == null)
            {
                return;
            }

            _uiDispatcher.BeginInvoke(
                new Action(
                    () =>
                    {
                        _chatViewModel.AddIncoming(message);
                    }));
        }
    }
}
