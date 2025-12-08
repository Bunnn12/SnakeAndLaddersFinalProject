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
        private readonly GameplayEventsHandler _eventsHandler;
        private readonly GameStateSynchronizer _gameStateSynchronizer;
        private readonly InventoryViewModel inventory;
        private readonly ILog _logger;
        private readonly Action _markServerEventReceived;
        private readonly Action<string, string, MessageBoxImage> _showMessage;
        private readonly Action<string> _setLastItemNotification;
        private readonly Action<int> _updateTurnTimerText;
        private readonly string _timeoutSkipMessage;
        private readonly string _timeoutKickMessage;
        private readonly string _gameWindowTitle;

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
            this._eventsHandler = eventsHandler ?? throw new ArgumentNullException(nameof(eventsHandler));
            this._gameStateSynchronizer = gameStateSynchronizer ?? throw new ArgumentNullException(nameof(gameStateSynchronizer));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._markServerEventReceived = markServerEventReceived ?? throw new ArgumentNullException(nameof(markServerEventReceived));
            this._showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
            this._setLastItemNotification = setLastItemNotification ?? throw new ArgumentNullException(nameof(setLastItemNotification));
            this._updateTurnTimerText = updateTurnTimerText ?? throw new ArgumentNullException(nameof(updateTurnTimerText));
            this._timeoutSkipMessage = timeoutSkipMessage ?? throw new ArgumentNullException(nameof(timeoutSkipMessage));
            this._timeoutKickMessage = timeoutKickMessage ?? throw new ArgumentNullException(nameof(timeoutKickMessage));
            this._gameWindowTitle = gameWindowTitle ?? throw new ArgumentNullException(nameof(gameWindowTitle));
        }

        public async Task HandlePlayerMovedAsync(PlayerMoveResultDto move)
        {
            if (move == null)
            {
                return;
            }

            _markServerEventReceived();

            Task handlerTask = _eventsHandler.HandleServerPlayerMovedAsync(move);

            if (move.MessageIndex.HasValue)
            {
                int messageIndex = move.MessageIndex.Value;

                string resourceKey = string.Format(
                    "Message{0}Text",
                    messageIndex);

                string messageText = Lang.ResourceManager.GetString(resourceKey);

                if (!string.IsNullOrWhiteSpace(messageText))
                {
                    _showMessage(
                        messageText,
                        _gameWindowTitle,
                        MessageBoxImage.Information);
                }
                else
                {
                    _logger.WarnFormat(
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

            _markServerEventReceived();

            Task handlerTask = _eventsHandler.HandleServerTurnChangedAsync(turnInfo);

            await ShowTurnChangedReasonAsync(turnInfo).ConfigureAwait(false);

            await _gameStateSynchronizer
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
                            _timeoutSkipMessage,
                            _gameWindowTitle,
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
                            _timeoutKickMessage,
                            _gameWindowTitle,
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

            _markServerEventReceived();

            _logger.InfoFormat(
                "HandleServerPlayerLeftAsync: GameId={0}, UserId={1}, Reason={2}",
                playerLeftInfo.GameId,
                playerLeftInfo.UserId,
                playerLeftInfo.Reason);

            Task handlerTask = _eventsHandler.HandleServerPlayerLeftAsync(playerLeftInfo);

            await _gameStateSynchronizer
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

            _markServerEventReceived();

            _logger.InfoFormat(
                "HandleServerItemUsedAsync: GameId={0}, ItemCode={1}, UserId={2}, TargetUserId={3}",
                notification.GameId,
                notification.ItemCode,
                notification.UserId,
                notification.TargetUserId);

            _setLastItemNotification(GameTextBuilder.BuildItemUsedMessage(notification));

            await inventory.InitializeAsync().ConfigureAwait(false);

            await _gameStateSynchronizer
                .SyncGameStateAsync(true)
                .ConfigureAwait(false);
        }

        public Task HandleTurnTimerUpdatedAsync(TurnTimerUpdateDto timerInfo)
        {
            if (timerInfo == null)
            {
                return Task.CompletedTask;
            }

            _markServerEventReceived();

            int seconds = timerInfo.RemainingSeconds;

            Application.Current.Dispatcher.Invoke(
                () => _updateTurnTimerText(seconds));

            return Task.CompletedTask;
        }
    }
}
