using log4net;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeAndLaddersFinalProject.Managers
{
    public sealed class DiceRollManager
    {
        private readonly int gameId;
        private readonly int localUserId;

        private readonly DiceSelectionManager diceSelectionManager;
        private readonly Func<IGameplayClient> gameplayClientProvider;
        private readonly ILog logger;

        private readonly Func<bool> getIsMyTurn;
        private readonly Func<bool> getIsAnimating;
        private readonly Func<bool> getIsRollRequestInProgress;
        private readonly Func<bool> getIsUseItemInProgress;
        private readonly Func<bool> getIsTargetSelectionActive;

        private readonly Action<bool> setIsRollRequestInProgress;
        private readonly Action raiseAllCanExecuteChanged;

        private readonly Func<Task> syncGameStateAsync;
        private readonly Func<Task> initializeInventoryAsync;
        private readonly Action markServerEventReceived;
        private readonly Func<Exception, string, bool> handleConnectionException;
        private readonly Action<string, string, MessageBoxImage> showMessage;

        private readonly string unknownErrorMessage;
        private readonly string rollDiceFailureMessagePrefix;
        private readonly string rollDiceUnexpectedErrorMessage;
        private readonly string gameWindowTitle;

        public DiceRollManager(
            int gameId,
            int localUserId,
            DiceSelectionManager diceSelectionManager,
            Func<IGameplayClient> gameplayClientProvider,
            ILog logger,
            Func<bool> getIsMyTurn,
            Func<bool> getIsAnimating,
            Func<bool> getIsRollRequestInProgress,
            Func<bool> getIsUseItemInProgress,
            Func<bool> getIsTargetSelectionActive,
            Action<bool> setIsRollRequestInProgress,
            Action raiseAllCanExecuteChanged,
            Func<Task> syncGameStateAsync,
            Func<Task> initializeInventoryAsync,
            Action markServerEventReceived,
            Func<Exception, string, bool> handleConnectionException,
            Action<string, string, MessageBoxImage> showMessage,
            string unknownErrorMessage,
            string rollDiceFailureMessagePrefix,
            string rollDiceUnexpectedErrorMessage,
            string gameWindowTitle)
        {
            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            if (localUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(localUserId));
            }

            this.gameId = gameId;
            this.localUserId = localUserId;
            this.diceSelectionManager = diceSelectionManager ?? throw new ArgumentNullException(nameof(diceSelectionManager));
            this.gameplayClientProvider = gameplayClientProvider ?? throw new ArgumentNullException(nameof(gameplayClientProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.getIsMyTurn = getIsMyTurn ?? throw new ArgumentNullException(nameof(getIsMyTurn));
            this.getIsAnimating = getIsAnimating ?? throw new ArgumentNullException(nameof(getIsAnimating));
            this.getIsRollRequestInProgress = getIsRollRequestInProgress ?? throw new ArgumentNullException(nameof(getIsRollRequestInProgress));
            this.getIsUseItemInProgress = getIsUseItemInProgress ?? throw new ArgumentNullException(nameof(getIsUseItemInProgress));
            this.getIsTargetSelectionActive = getIsTargetSelectionActive ?? throw new ArgumentNullException(nameof(getIsTargetSelectionActive));
            this.setIsRollRequestInProgress = setIsRollRequestInProgress ?? throw new ArgumentNullException(nameof(setIsRollRequestInProgress));
            this.raiseAllCanExecuteChanged = raiseAllCanExecuteChanged ?? throw new ArgumentNullException(nameof(raiseAllCanExecuteChanged));
            this.syncGameStateAsync = syncGameStateAsync ?? throw new ArgumentNullException(nameof(syncGameStateAsync));
            this.initializeInventoryAsync = initializeInventoryAsync ?? throw new ArgumentNullException(nameof(initializeInventoryAsync));
            this.markServerEventReceived = markServerEventReceived ?? throw new ArgumentNullException(nameof(markServerEventReceived));
            this.handleConnectionException = handleConnectionException ?? throw new ArgumentNullException(nameof(handleConnectionException));
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
            this.unknownErrorMessage = unknownErrorMessage ?? throw new ArgumentNullException(nameof(unknownErrorMessage));
            this.rollDiceFailureMessagePrefix = rollDiceFailureMessagePrefix ?? throw new ArgumentNullException(nameof(rollDiceFailureMessagePrefix));
            this.rollDiceUnexpectedErrorMessage = rollDiceUnexpectedErrorMessage ?? throw new ArgumentNullException(nameof(rollDiceUnexpectedErrorMessage));
            this.gameWindowTitle = gameWindowTitle ?? throw new ArgumentNullException(nameof(gameWindowTitle));
        }

        public bool CanRollDice()
        {
            logger.InfoFormat(
                "CanRollDice: gameId={0}, localUserId={1}, isMyTurn={2}, isAnimating={3}, isRollRequestInProgress={4}",
                gameId,
                localUserId,
                getIsMyTurn(),
                getIsAnimating(),
                getIsRollRequestInProgress());

            return PlayerActionGuard.CanRollDice(
                getIsMyTurn(),
                getIsAnimating(),
                getIsRollRequestInProgress(),
                getIsUseItemInProgress(),
                getIsTargetSelectionActive());
        }

        public async Task RollDiceForLocalPlayerAsync()
        {
            if (getIsRollRequestInProgress())
            {
                return;
            }

            setIsRollRequestInProgress(true);
            raiseAllCanExecuteChanged();

            try
            {
                IGameplayClient client = gameplayClientProvider();

                if (client == null)
                {
                    return;
                }

                byte? diceSlotNumber = diceSelectionManager.SelectedSlot;

                RollDiceResponseDto response = await client
                    .GetRollDiceAsync(gameId, localUserId, diceSlotNumber)
                    .ConfigureAwait(false);

                if (response == null || !response.Success)
                {
                    string failureReason = response != null && !string.IsNullOrWhiteSpace(response.FailureReason)
                        ? response.FailureReason
                        : unknownErrorMessage;

                    logger.Warn("RollDice failed: " + failureReason);

                    showMessage(
                        rollDiceFailureMessagePrefix + failureReason,
                        gameWindowTitle,
                        MessageBoxImage.Warning);

                    return;
                }

                markServerEventReceived();
                await ShowGrantedRewardsAsync(response, gameWindowTitle).ConfigureAwait(false);

                diceSelectionManager.ResetSelection();

                logger.InfoFormat(
                    "RollDice request accepted. UserId={0}, From={1}, To={2}, Dice={3}",
                    localUserId,
                    response.FromCellIndex,
                    response.ToCellIndex,
                    response.DiceValue);

                await syncGameStateAsync().ConfigureAwait(false);
                await initializeInventoryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (handleConnectionException(
                        ex,
                        "Connection lost while rolling dice."))
                {
                    return;
                }

                logger.Error(rollDiceUnexpectedErrorMessage, ex);

                showMessage(
                    rollDiceUnexpectedErrorMessage,
                    gameWindowTitle,
                    MessageBoxImage.Error);
            }
            finally
            {
                setIsRollRequestInProgress(false);
                raiseAllCanExecuteChanged();
            }
        }

        private static async Task ShowGrantedRewardsAsync(
            RollDiceResponseDto response,
            string gameWindowTitle)
        {
            if (string.IsNullOrWhiteSpace(response.GrantedItemCode) &&
                string.IsNullOrWhiteSpace(response.GrantedDiceCode))
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    string text = "¡Has obtenido ";

                    if (!string.IsNullOrWhiteSpace(response.GrantedItemCode))
                    {
                        text += string.Format("un ítem ({0})", response.GrantedItemCode);
                    }

                    if (!string.IsNullOrWhiteSpace(response.GrantedDiceCode))
                    {
                        if (!string.IsNullOrWhiteSpace(response.GrantedItemCode))
                        {
                            text += " y ";
                        }

                        text += string.Format("un dado ({0})", response.GrantedDiceCode);
                    }

                    text += "!";

                    MessageBox.Show(
                        text,
                        gameWindowTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
        }
    }
}
