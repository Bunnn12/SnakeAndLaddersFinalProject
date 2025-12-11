using log4net;
using SnakeAndLaddersFinalProject.GameBoardService;
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
            int gameId,
            ILog logger,
            Func<IGameplayClient> gameplayClientProvider,
            Action markServerEventReceived,
            Func<GetGameStateResponseDto, bool, Task> applyGameStateAsync,
            Func<Exception, string, bool> handleConnectionException,
            string syncErrorLogMessage,
            string errorDialogTitle,
            Action<string, string, MessageBoxImage> showErrorMessage)
        {
            if (gameId <= 0) throw new ArgumentOutOfRangeException(nameof(gameId));

            _gameId = gameId;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gameplayClientProvider = gameplayClientProvider ?? throw new ArgumentNullException(
                nameof(gameplayClientProvider));
            _markServerEventReceived = markServerEventReceived ?? throw new ArgumentNullException(
                nameof(markServerEventReceived));
            _applyGameStateAsync = applyGameStateAsync ?? throw new ArgumentNullException(
                nameof(applyGameStateAsync));
            _handleConnectionException = handleConnectionException ??
                throw new ArgumentNullException(nameof(handleConnectionException));
            _syncErrorLogMessage = syncErrorLogMessage ?? throw new ArgumentNullException(
                nameof(syncErrorLogMessage));
            _errorDialogTitle = errorDialogTitle ?? throw new ArgumentNullException(
                nameof(errorDialogTitle));
            _showErrorMessage = showErrorMessage ?? throw new ArgumentNullException(
                nameof(showErrorMessage));
        }

        public async Task SyncGameStateAsync(bool forceUpdateTokenPositions)
        {
            IGameplayClient client = _gameplayClientProvider();
            if (client == null)
            {
                return;
            }

            try
            {
                GetGameStateResponseDto stateResponse = await client.GetGameStateAsync(_gameId)
                    .ConfigureAwait(false);
                if (stateResponse == null)
                {
                    return;
                }

                _markServerEventReceived();
                await _applyGameStateAsync(stateResponse, forceUpdateTokenPositions)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_handleConnectionException(ex, "Connection lost while syncing game state."))
                {
                    return;
                }

                ExceptionHandler.Handle(ex, "GameBoardViewModel.SyncGameStateAsync", _logger);
                _showErrorMessage(_syncErrorLogMessage, _errorDialogTitle, MessageBoxImage.Error);
            }
        }
    }
}
