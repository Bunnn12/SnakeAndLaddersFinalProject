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
        private readonly ChatViewModel chatViewModel;
        private readonly Dispatcher uiDispatcher;

        public ChatClientCallback(ChatViewModel chatViewModelValue)
        {
            chatViewModel = chatViewModelValue ?? throw new ArgumentNullException(nameof(chatViewModelValue));
            uiDispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public void OnMessage(int lobbyId, ChatMessageDto message)
        {
            if (message == null)
            {
                return;
            }

            uiDispatcher.BeginInvoke(
                new Action(
                    () =>
                    {
                        chatViewModel.AddIncoming(message);
                    }));
        }
    }
}
