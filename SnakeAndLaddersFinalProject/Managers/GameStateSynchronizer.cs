using log4net;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Utilities;
using SnakeAndLaddersFinalProject.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeAndLaddersFinalProject.Managers
{
    public sealed class GameStateSynchronizer
    {
        private const string CONNECTION_LOST_MESSAGE =
            "Connection lost while syncing game state.";

        private const string SYNC_EXCEPTION_CONTEXT =
            "GameStateSynchronizer.SyncGameStateAsync";

        private const string LOG_CLIENT_NULL_MESSAGE =
            "IGameplayClient provider returned null in SyncGameStateAsync. GameId={0}.";

        private const string LOG_STATE_NULL_MESSAGE =
            "GetGameStateAsync returned null state for GameId={0}.";

        private readonly int _gameId;
        private readonly ILog _logger;
        private readonly Func<IGameplayClient> _gameplayClientProvider;
        private readonly Action _markServerEventReceived;
        private readonly Func<GetGameStateResponseDto, bool, Task> _applyGameStateAsync;
        private readonly Func<Exception, string, bool> _handleConnectionException;
        private readonly string _syncErrorLogMessage;
        private readonly string _errorDialogTitle;
        private readonly Action<string, string, MessageBoxImage> _showErrorMessage;

        public GameStateSynchronizer(
            GameStateSynchronizerDependencies dependencies,
            GameStateSynchronizerUiConfig uiConfig)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            if (uiConfig == null)
            {
                throw new ArgumentNullException(nameof(uiConfig));
            }

            if (dependencies.GameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dependencies.GameId));
            }

            _gameId = dependencies.GameId;
            _logger = dependencies.Logger
                ?? throw new ArgumentNullException(nameof(dependencies));
            _gameplayClientProvider = dependencies.GameplayClientProvider
                ?? throw new ArgumentNullException(nameof(dependencies));
            _markServerEventReceived = dependencies.MarkServerEventReceived
                ?? throw new ArgumentNullException(nameof(dependencies));
            _applyGameStateAsync = dependencies.ApplyGameStateAsync
                ?? throw new ArgumentNullException(nameof(dependencies));
            _handleConnectionException = dependencies.HandleConnectionException
                ?? throw new ArgumentNullException(nameof(dependencies));

            _syncErrorLogMessage = uiConfig.SyncErrorLogMessage
                ?? throw new ArgumentNullException(nameof(uiConfig));
            _errorDialogTitle = uiConfig.ErrorDialogTitle
                ?? throw new ArgumentNullException(nameof(uiConfig));
            _showErrorMessage = uiConfig.ShowErrorMessage
                ?? throw new ArgumentNullException(nameof(uiConfig));
        }

        public async Task SyncGameStateAsync(bool forceUpdateTokenPositions)
        {
            IGameplayClient client = _gameplayClientProvider();
            if (client == null)
            {
                _logger.WarnFormat(LOG_CLIENT_NULL_MESSAGE, _gameId);
                return;
            }

            try
            {
                GetGameStateResponseDto stateResponse =
                    await client.GetGameStateAsync(_gameId).ConfigureAwait(false);

                if (stateResponse == null)
                {
                    _logger.WarnFormat(LOG_STATE_NULL_MESSAGE, _gameId);
                    return;
                }

                _markServerEventReceived();

                await _applyGameStateAsync(stateResponse, forceUpdateTokenPositions)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                bool isConnectionIssue =
                    _handleConnectionException(ex, CONNECTION_LOST_MESSAGE);

                if (isConnectionIssue)
                {
                    return;
                }

                ExceptionHandler.Handle(ex, SYNC_EXCEPTION_CONTEXT, _logger);

                _showErrorMessage(
                    _syncErrorLogMessage,
                    _errorDialogTitle,
                    MessageBoxImage.Error);
            }
        }
    }

    public sealed class GameStateSynchronizerDependencies
    {
        public GameStateSynchronizerDependencies(
            int gameId,
            ILog logger,
            Func<IGameplayClient> gameplayClientProvider,
            Action markServerEventReceived,
            Func<GetGameStateResponseDto, bool, Task> applyGameStateAsync,
            Func<Exception, string, bool> handleConnectionException)
        {
            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            GameplayClientProvider = gameplayClientProvider
                ?? throw new ArgumentNullException(nameof(gameplayClientProvider));
            MarkServerEventReceived = markServerEventReceived
                ?? throw new ArgumentNullException(nameof(markServerEventReceived));
            ApplyGameStateAsync = applyGameStateAsync
                ?? throw new ArgumentNullException(nameof(applyGameStateAsync));
            HandleConnectionException = handleConnectionException
                ?? throw new ArgumentNullException(nameof(handleConnectionException));

            GameId = gameId;
        }

        public int GameId { get; }

        public ILog Logger { get; }

        public Func<IGameplayClient> GameplayClientProvider { get; }

        public Action MarkServerEventReceived { get; }

        public Func<GetGameStateResponseDto, bool, Task> ApplyGameStateAsync { get; }

        public Func<Exception, string, bool> HandleConnectionException { get; }
    }

    public sealed class GameStateSynchronizerUiConfig
    {
        public GameStateSynchronizerUiConfig(
            string syncErrorLogMessage,
            string errorDialogTitle,
            Action<string, string, MessageBoxImage> showErrorMessage)
        {
            SyncErrorLogMessage = syncErrorLogMessage
                ?? throw new ArgumentNullException(nameof(syncErrorLogMessage));
            ErrorDialogTitle = errorDialogTitle
                ?? throw new ArgumentNullException(nameof(errorDialogTitle));
            ShowErrorMessage = showErrorMessage
                ?? throw new ArgumentNullException(nameof(showErrorMessage));
        }

        public string SyncErrorLogMessage { get; }

        public string ErrorDialogTitle { get; }

        public Action<string, string, MessageBoxImage> ShowErrorMessage { get; }
    }
}
