using log4net;
using SnakeAndLaddersFinalProject.Game.Gameplay;
using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeAndLaddersFinalProject.Managers
{
    public sealed class GameplayServerEventsRouter
    {
        private readonly GameplayEventsHandler eventsHandler;
        private readonly GameStateSynchronizer gameStateSynchronizer;
        private readonly InventoryViewModel inventory;
        private readonly ILog logger;
        private readonly Action markServerEventReceived;
        private readonly Action<string, string, MessageBoxImage> showMessage;
        private readonly Action<string> setLastItemNotification;
        private readonly Action<int> updateTurnTimerText;
        private readonly string timeoutSkipMessage;
        private readonly string timeoutKickMessage;
        private readonly string gameWindowTitle;

        public GameplayServerEventsRouter(
            GameplayEventsHandler eventsHandler,
            GameStateSynchronizer gameStateSynchronizer,
            InventoryViewModel inventory,
            ILog logger,
            Action markServerEventReceived,
            Action<string, string, MessageBoxImage> showMessage,
            Action<string> setLastItemNotification,
            Action<int> updateTurnTimerText,
            string timeoutSkipMessage,
            string timeoutKickMessage,
            string gameWindowTitle)
        {
            this.eventsHandler = eventsHandler ?? throw new ArgumentNullException(nameof(eventsHandler));
            this.gameStateSynchronizer = gameStateSynchronizer ?? throw new ArgumentNullException(nameof(gameStateSynchronizer));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.markServerEventReceived = markServerEventReceived ?? throw new ArgumentNullException(nameof(markServerEventReceived));
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
            this.setLastItemNotification = setLastItemNotification ?? throw new ArgumentNullException(nameof(setLastItemNotification));
            this.updateTurnTimerText = updateTurnTimerText ?? throw new ArgumentNullException(nameof(updateTurnTimerText));
            this.timeoutSkipMessage = timeoutSkipMessage ?? throw new ArgumentNullException(nameof(timeoutSkipMessage));
            this.timeoutKickMessage = timeoutKickMessage ?? throw new ArgumentNullException(nameof(timeoutKickMessage));
            this.gameWindowTitle = gameWindowTitle ?? throw new ArgumentNullException(nameof(gameWindowTitle));
        }

        public async Task HandlePlayerMovedAsync(PlayerMoveResultDto move)
        {
            if (move == null)
            {
                return;
            }

            markServerEventReceived();

            Task handlerTask = eventsHandler.HandleServerPlayerMovedAsync(move);

            if (move.MessageIndex.HasValue)
            {
                int messageIndex = move.MessageIndex.Value;

                string resourceKey = string.Format(
                    "Message{0}Text",
                    messageIndex);

                string messageText = Lang.ResourceManager.GetString(resourceKey);

                if (!string.IsNullOrWhiteSpace(messageText))
                {
                    showMessage(
                        messageText,
                        gameWindowTitle,
                        MessageBoxImage.Information);
                }
                else
                {
                    logger.WarnFormat(
                        "No se encontró recurso de mensaje para key={0}.",
                        resourceKey);
                }
            }

            await handlerTask.ConfigureAwait(false);
        }

        public async Task HandleTurnChangedAsync(TurnChangedDto turnInfo)
        {
            if (turnInfo == null)
            {
                return;
            }

            markServerEventReceived();

            Task handlerTask = eventsHandler.HandleServerTurnChangedAsync(turnInfo);

            await ShowTurnChangedReasonAsync(turnInfo).ConfigureAwait(false);

            await gameStateSynchronizer
                .SyncGameStateAsync(false)
                .ConfigureAwait(false);

            await handlerTask.ConfigureAwait(false);
        }

        private async Task ShowTurnChangedReasonAsync(TurnChangedDto turnInfo)
        {
            if (string.IsNullOrWhiteSpace(turnInfo.Reason))
            {
                return;
            }

            string normalizedReason = turnInfo.Reason.Trim().ToUpperInvariant();

            if (normalizedReason == "TIMEOUT_SKIP")
            {
                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        MessageBox.Show(
                            timeoutSkipMessage,
                            gameWindowTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
            }
            else if (normalizedReason == "TIMEOUT_KICK")
            {
                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        MessageBox.Show(
                            timeoutKickMessage,
                            gameWindowTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
            }
        }

        public async Task HandlePlayerLeftAsync(PlayerLeftDto playerLeftInfo)
        {
            if (playerLeftInfo == null)
            {
                return;
            }

            markServerEventReceived();

            logger.InfoFormat(
                "HandleServerPlayerLeftAsync: GameId={0}, UserId={1}, Reason={2}",
                playerLeftInfo.GameId,
                playerLeftInfo.UserId,
                playerLeftInfo.Reason);

            Task handlerTask = eventsHandler.HandleServerPlayerLeftAsync(playerLeftInfo);

            await gameStateSynchronizer
                .SyncGameStateAsync(false)
                .ConfigureAwait(false);

            await handlerTask.ConfigureAwait(false);
        }

        public async Task HandleItemUsedAsync(ItemUsedNotificationDto notification)
        {
            if (notification == null)
            {
                return;
            }

            markServerEventReceived();

            logger.InfoFormat(
                "HandleServerItemUsedAsync: GameId={0}, ItemCode={1}, UserId={2}, TargetUserId={3}",
                notification.GameId,
                notification.ItemCode,
                notification.UserId,
                notification.TargetUserId);

            setLastItemNotification(GameTextBuilder.BuildItemUsedMessage(notification));

            await inventory.InitializeAsync().ConfigureAwait(false);

            await gameStateSynchronizer
                .SyncGameStateAsync(true)
                .ConfigureAwait(false);
        }

        public Task HandleTurnTimerUpdatedAsync(TurnTimerUpdateDto timerInfo)
        {
            if (timerInfo == null)
            {
                return Task.CompletedTask;
            }

            markServerEventReceived();

            int seconds = timerInfo.RemainingSeconds;

            Application.Current.Dispatcher.Invoke(
                () => updateTurnTimerText(seconds));

            return Task.CompletedTask;
        }
    }
}
