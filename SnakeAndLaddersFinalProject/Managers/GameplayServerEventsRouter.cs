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
        private const string MESSAGE_RESOURCE_KEY_FORMAT = "Message{0}Text";
        private const string REASON_TIMEOUT_SKIP = "TIMEOUT_SKIP";
        private const string REASON_TIMEOUT_KICK = "TIMEOUT_KICK";

        private const string LOG_PLAYER_LEFT_FORMAT =
            "HandleServerPlayerLeftAsync: GameId={0}, UserId={1}, Reason={2}";

        private const string LOG_ITEM_USED_FORMAT =
            "HandleServerItemUsedAsync: GameId={0}, ItemCode={1}, UserId={2}, TargetUserId={3}";

        private readonly GameplayEventsHandler _eventsHandler;
        private readonly GameStateSynchronizer _gameStateSynchronizer;
        private readonly InventoryViewModel _inventory;
        private readonly ILog _logger;
        private readonly Action _markServerEventReceived;
        private readonly Action<string, string, MessageBoxImage> _showMessage;
        private readonly Action<string> _setLastItemNotification;
        private readonly Action<int> _updateTurnTimerText;
        private readonly string _timeoutSkipMessage;
        private readonly string _timeoutKickMessage;
        private readonly string _gameWindowTitle;

        public GameplayServerEventsRouter(
            GameplayServerEventsRouterDependencies dependencies,
            GameplayServerEventsRouterUiConfig uiConfig)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            if (uiConfig == null)
            {
                throw new ArgumentNullException(nameof(uiConfig));
            }

            _eventsHandler = dependencies.EventsHandler
                ?? throw new ArgumentNullException(nameof(dependencies));

            _gameStateSynchronizer = dependencies.GameStateSynchronizer
                ?? throw new ArgumentNullException(nameof(dependencies));

            _inventory = dependencies.Inventory
                ?? throw new ArgumentNullException(nameof(dependencies));

            _logger = dependencies.Logger
                ?? throw new ArgumentNullException(nameof(dependencies));

            _markServerEventReceived = uiConfig.MarkServerEventReceived
                ?? throw new ArgumentNullException(nameof(uiConfig));

            _showMessage = uiConfig.ShowMessage
                ?? throw new ArgumentNullException(nameof(uiConfig));

            _setLastItemNotification = uiConfig.SetLastItemNotification
                ?? throw new ArgumentNullException(nameof(uiConfig));

            _updateTurnTimerText = uiConfig.UpdateTurnTimerText
                ?? throw new ArgumentNullException(nameof(uiConfig));

            _timeoutSkipMessage = uiConfig.TimeoutSkipMessage
                ?? throw new ArgumentNullException(nameof(uiConfig));

            _timeoutKickMessage = uiConfig.TimeoutKickMessage
                ?? throw new ArgumentNullException(nameof(uiConfig));

            _gameWindowTitle = uiConfig.GameWindowTitle
                ?? throw new ArgumentNullException(nameof(uiConfig));
        }

        public async Task HandlePlayerMovedAsync(PlayerMoveResultDto move)
        {
            if (move == null)
            {
                return;
            }

            _markServerEventReceived();

            Task handlerTask = _eventsHandler.HandleServerPlayerMovedAsync(move);

            await ShowMessageCellIfNeededAsync(move).ConfigureAwait(false);

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
            await _gameStateSynchronizer.SyncGameStateAsync(false).ConfigureAwait(false);

            await handlerTask.ConfigureAwait(false);
        }

        public async Task HandlePlayerLeftAsync(PlayerLeftDto playerLeftInfo)
        {
            if (playerLeftInfo == null)
            {
                return;
            }

            _markServerEventReceived();

            _logger.InfoFormat(
                LOG_PLAYER_LEFT_FORMAT,
                playerLeftInfo.GameId,
                playerLeftInfo.UserId,
                playerLeftInfo.Reason);

            Task handlerTask = _eventsHandler.HandleServerPlayerLeftAsync(playerLeftInfo);

            await _gameStateSynchronizer.SyncGameStateAsync(false).ConfigureAwait(false);
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
                LOG_ITEM_USED_FORMAT,
                notification.GameId,
                notification.ItemCode,
                notification.UserId,
                notification.TargetUserId);

            _setLastItemNotification(GameTextBuilder.BuildItemUsedMessage(notification));

            await _inventory.InitializeAsync().ConfigureAwait(false);
            await _gameStateSynchronizer.SyncGameStateAsync(true).ConfigureAwait(false);
        }

        public Task HandleTurnTimerUpdatedAsync(TurnTimerUpdateDto timerInfo)
        {
            if (timerInfo == null)
            {
                return Task.CompletedTask;
            }

            _markServerEventReceived();

            int remainingSeconds = timerInfo.RemainingSeconds;

            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher == null)
            {
                _updateTurnTimerText(remainingSeconds);
                return Task.CompletedTask;
            }

            if (dispatcher.CheckAccess())
            {
                _updateTurnTimerText(remainingSeconds);
            }
            else
            {
                dispatcher.BeginInvoke(
                    new Action(
                        () => _updateTurnTimerText(remainingSeconds)));
            }

            return Task.CompletedTask;
        }

        private async Task ShowMessageCellIfNeededAsync(PlayerMoveResultDto move)
        {
            if (!move.MessageIndex.HasValue)
            {
                return;
            }

            int messageIndex = move.MessageIndex.Value;

            string resourceKey = string.Format(
                MESSAGE_RESOURCE_KEY_FORMAT,
                messageIndex);

            string messageText = Lang.ResourceManager.GetString(resourceKey);

            if (string.IsNullOrWhiteSpace(messageText))
            {
                _logger.WarnFormat(
                    "No se encontró recurso de mensaje para key={0}.",
                    resourceKey);
                return;
            }

            await ShowInformationMessageAsync(messageText).ConfigureAwait(false);
        }

        private async Task ShowTurnChangedReasonAsync(TurnChangedDto turnInfo)
        {
            if (string.IsNullOrWhiteSpace(turnInfo.Reason))
            {
                return;
            }

            string normalizedReason = turnInfo.Reason.Trim().ToUpperInvariant();
            string messageToShow = GetTimeoutMessage(normalizedReason);

            if (string.IsNullOrWhiteSpace(messageToShow))
            {
                return;
            }

            await ShowInformationMessageAsync(messageToShow).ConfigureAwait(false);
        }

        private string GetTimeoutMessage(string normalizedReason)
        {
            if (normalizedReason == REASON_TIMEOUT_SKIP)
            {
                return _timeoutSkipMessage;
            }

            if (normalizedReason == REASON_TIMEOUT_KICK)
            {
                return _timeoutKickMessage;
            }

            return string.Empty;
        }

        private Task ShowInformationMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Task.CompletedTask;
            }

            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher == null)
            {
                _showMessage(message, _gameWindowTitle, MessageBoxImage.Information);
                return Task.CompletedTask;
            }

            if (dispatcher.CheckAccess())
            {
                _showMessage(message, _gameWindowTitle, MessageBoxImage.Information);
            }
            else
            {
                dispatcher.BeginInvoke(
                    new Action(
                        () => _showMessage(
                            message,
                            _gameWindowTitle,
                            MessageBoxImage.Information)));
            }

            return Task.CompletedTask;
        }
    }

    public sealed class GameplayServerEventsRouterDependencies
    {
        public GameplayServerEventsRouterDependencies(
            GameplayEventsHandler eventsHandler,
            GameStateSynchronizer gameStateSynchronizer,
            InventoryViewModel inventory,
            ILog logger)
        {
            EventsHandler = eventsHandler
                ?? throw new ArgumentNullException(nameof(eventsHandler));
            GameStateSynchronizer = gameStateSynchronizer
                ?? throw new ArgumentNullException(nameof(gameStateSynchronizer));
            Inventory = inventory
                ?? throw new ArgumentNullException(nameof(inventory));
            Logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public GameplayEventsHandler EventsHandler { get; }

        public GameStateSynchronizer GameStateSynchronizer { get; }

        public InventoryViewModel Inventory { get; }

        public ILog Logger { get; }
    }

    public sealed class GameplayServerEventsRouterUiConfig
    {
        public GameplayServerEventsRouterUiConfig(
            Action markServerEventReceived,
            Action<string, string, MessageBoxImage> showMessage,
            Action<string> setLastItemNotification,
            Action<int> updateTurnTimerText,
            string timeoutSkipMessage,
            string timeoutKickMessage,
            string gameWindowTitle)
        {
            MarkServerEventReceived = markServerEventReceived
                ?? throw new ArgumentNullException(nameof(markServerEventReceived));
            ShowMessage = showMessage
                ?? throw new ArgumentNullException(nameof(showMessage));
            SetLastItemNotification = setLastItemNotification
                ?? throw new ArgumentNullException(nameof(setLastItemNotification));
            UpdateTurnTimerText = updateTurnTimerText
                ?? throw new ArgumentNullException(nameof(updateTurnTimerText));
            TimeoutSkipMessage = timeoutSkipMessage
                ?? throw new ArgumentNullException(nameof(timeoutSkipMessage));
            TimeoutKickMessage = timeoutKickMessage
                ?? throw new ArgumentNullException(nameof(timeoutKickMessage));
            GameWindowTitle = gameWindowTitle
                ?? throw new ArgumentNullException(nameof(gameWindowTitle));
        }

        public Action MarkServerEventReceived { get; }

        public Action<string, string, MessageBoxImage> ShowMessage { get; }

        public Action<string> SetLastItemNotification { get; }

        public Action<int> UpdateTurnTimerText { get; }

        public string TimeoutSkipMessage { get; }

        public string TimeoutKickMessage { get; }

        public string GameWindowTitle { get; }
    }
}
