using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.LobbyService;

namespace SnakeAndLaddersFinalProject.Services
{
    [CallbackBehavior(
        UseSynchronizationContext = false,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal sealed class LobbyClientCallback : ILobbyServiceCallback
    {
        private readonly ILobbyEventsHandler handler;
        private readonly Dispatcher dispatcher;

        public LobbyClientCallback(ILobbyEventsHandler handler)
        {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
            dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public void OnLobbyUpdated(LobbyInfo lobby)
        {
            if (lobby == null)
            {
                return;
            }

            RunOnUiThreadAsync(() => handler.HandleLobbyUpdatedAsync(lobby));
        }

        public void OnLobbyClosed(int partidaId, string reason)
        {
            RunOnUiThreadAsync(() => handler.HandleLobbyClosedAsync(partidaId, reason));
        }

        public void OnKickedFromLobby(int partidaId, string reason)
        {
            RunOnUiThreadAsync(() => handler.HandleKickedFromLobbyAsync(partidaId, reason));
        }

        public void OnPublicLobbiesChanged(LobbySummary[] lobbies)
        {
            IList<LobbySummary> list = lobbies ?? Array.Empty<LobbySummary>();
            RunOnUiThreadAsync(() => handler.HandlePublicLobbiesChangedAsync(list));
        }

        private void RunOnUiThreadAsync(Func<Task> actionAsync)
        {
            if (actionAsync == null)
            {
                return;
            }

            if (dispatcher.CheckAccess())
            {
                _ = actionAsync();
                return;
            }

            dispatcher.BeginInvoke(
                new Action(
                    () =>
                    {
                        _ = actionAsync();
                    }));
        }
    }
}
