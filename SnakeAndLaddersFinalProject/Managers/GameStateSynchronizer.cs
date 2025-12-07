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
        private readonly int gameId;
        private readonly ILog logger;
        private readonly Func<IGameplayClient> gameplayClientProvider;
        private readonly Action markServerEventReceived;
        private readonly Func<GetGameStateResponseDto, bool, Task> applyGameStateAsync;
        private readonly Func<Exception, string, bool> handleConnectionException;
        private readonly string syncErrorLogMessage;
        private readonly string errorDialogTitle;
        private readonly Action<string, string, MessageBoxImage> showErrorMessage;

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
            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            this.gameId = gameId;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameplayClientProvider = gameplayClientProvider ?? throw new ArgumentNullException(nameof(gameplayClientProvider));
            this.markServerEventReceived = markServerEventReceived ?? throw new ArgumentNullException(nameof(markServerEventReceived));
            this.applyGameStateAsync = applyGameStateAsync ?? throw new ArgumentNullException(nameof(applyGameStateAsync));
            this.handleConnectionException = handleConnectionException ?? throw new ArgumentNullException(nameof(handleConnectionException));
            this.syncErrorLogMessage = syncErrorLogMessage ?? throw new ArgumentNullException(nameof(syncErrorLogMessage));
            this.errorDialogTitle = errorDialogTitle ?? throw new ArgumentNullException(nameof(errorDialogTitle));
            this.showErrorMessage = showErrorMessage ?? throw new ArgumentNullException(nameof(showErrorMessage));
        }

        public async Task SyncGameStateAsync(bool forceUpdateTokenPositions)
        {
            IGameplayClient client = gameplayClientProvider();

            if (client == null)
            {
                return;
            }

            try
            {
                GetGameStateResponseDto stateResponse = await client
                    .GetGameStateAsync(gameId)
                    .ConfigureAwait(false);

                if (stateResponse == null)
                {
                    return;
                }

                markServerEventReceived();

                await applyGameStateAsync(
                        stateResponse,
                        forceUpdateTokenPositions)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (handleConnectionException(
                        ex,
                        "Connection lost while syncing game state."))
                {
                    return;
                }

                ExceptionHandler.Handle(
                    ex,
                    "GameBoardViewModel.SyncGameStateAsync",
                    logger);

                showErrorMessage(
                    syncErrorLogMessage,
                    errorDialogTitle,
                    MessageBoxImage.Error);
            }
        }
    }
}
