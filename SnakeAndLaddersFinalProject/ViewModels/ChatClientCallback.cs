using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Threading;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    /// <summary>
    /// Recibe push del server y lo reenvía al hilo de UI.
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)]
    public sealed class ChatClientCallback : IChatServiceCallback
    {
        private readonly ChatViewModel _vm;
        private readonly Dispatcher _ui;

        public ChatClientCallback(ChatViewModel vm)
        {
            _vm = vm;
            _ui = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public void OnMessage(int lobbyId, ChatMessageDto message)
        {
            if (message == null) return;

            _ui.BeginInvoke(new Action(() =>
            {
                _vm.AddIncoming(message);
            }));
        }
    }
}
