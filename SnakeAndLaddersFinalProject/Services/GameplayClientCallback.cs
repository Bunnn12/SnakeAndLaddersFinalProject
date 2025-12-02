
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
        private readonly IGameplayEventsHandler _eventsHandler;
        private readonly Dispatcher _dispatcher;

        public GameplayClientCallback(IGameplayEventsHandler eventsHandler)
        {
            this._eventsHandler = eventsHandler
                ?? throw new ArgumentNullException(nameof(eventsHandler));

            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public void OnPlayerMoved(PlayerMoveResultDto move)
        {
            if (move == null)
            {
                return;
            }

            RunOnUiThreadAsync(() => _eventsHandler.HandleServerPlayerMovedAsync(move));
        }

        public void OnTurnChanged(TurnChangedDto turnInfo)
        {
            if (turnInfo == null)
            {
                return;
            }

            RunOnUiThreadAsync(() => _eventsHandler.HandleServerTurnChangedAsync(turnInfo));
        }

        public void OnPlayerLeft(PlayerLeftDto playerLeftInfo)
        {
            if (playerLeftInfo == null)
            {
                return;
            }

            RunOnUiThreadAsync(() => _eventsHandler.HandleServerPlayerLeftAsync(playerLeftInfo));
        }

        public void OnItemUsed(ItemUsedNotificationDto notification)
        {
            if (notification == null)
            {
                return;
            }

            RunOnUiThreadAsync(() => _eventsHandler.HandleServerItemUsedAsync(notification));
        }

        private void RunOnUiThreadAsync(Func<Task> actionAsync)
        {
            if (actionAsync == null)
            {
                return;
            }

            if (_dispatcher.CheckAccess())
            {
                _ = actionAsync();
                return;
            }

            _dispatcher.BeginInvoke(
                new Action(
                    () =>
                    {
                        _ = actionAsync();
                    }));
        }
    }
}
