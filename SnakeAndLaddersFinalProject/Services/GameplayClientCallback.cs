using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;

namespace SnakeAndLaddersFinalProject.Services
{
    internal sealed class GameplayClientCallback : IGameplayServiceCallback
    {
        private readonly IGameplayEventsHandler eventsHandler;
        private readonly Dispatcher dispatcher;

        public GameplayClientCallback(IGameplayEventsHandler eventsHandler)
        {
            this.eventsHandler = eventsHandler
                ?? throw new ArgumentNullException(nameof(eventsHandler));

            dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public void OnPlayerMoved(PlayerMoveResultDto move)
        {
            if (move == null)
            {
                return;
            }

            RunOnUiThreadAsync(() => eventsHandler.HandleServerPlayerMovedAsync(move));
        }

        public void OnTurnChanged(TurnChangedDto turnInfo)
        {
            if (turnInfo == null)
            {
                return;
            }

            RunOnUiThreadAsync(() => eventsHandler.HandleServerTurnChangedAsync(turnInfo));
        }

        public void OnPlayerLeft(PlayerLeftDto playerLeftInfo)
        {
            if (playerLeftInfo == null)
            {
                return;
            }

            RunOnUiThreadAsync(() => eventsHandler.HandleServerPlayerLeftAsync(playerLeftInfo));
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
