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
        private readonly int _gameId;
        private readonly int _localUserId;
        private readonly DiceSelectionManager _diceSelectionManager;
        private readonly Func<IGameplayClient> _gameplayClientProvider;
        private readonly ILog _logger;
        private readonly Func<bool> _getIsMyTurn;
        private readonly Func<bool> _getIsAnimating;
        private readonly Func<bool> _getIsRollRequestInProgress;
        private readonly Func<bool> _getIsUseItemInProgress;
        private readonly Func<bool> _getIsTargetSelectionActive;
        private readonly Action<bool> _setIsRollRequestInProgress;
        private readonly Action _raiseAllCanExecuteChanged;
        private readonly Func<Task> _syncGameStateAsync;
        private readonly Func<Task> _initializeInventoryAsync;
        private readonly Action _markServerEventReceived;
        private readonly Func<Exception, string, bool> _handleConnectionException;
        private readonly Action<string, string, MessageBoxImage> _showMessage;
        private readonly string _unknownErrorMessage;
        private readonly string _rollDiceFailureMessagePrefix;
        private readonly string _rollDiceUnexpectedErrorMessage;
        private readonly string _gameWindowTitle;

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
            if (gameId <= 0) throw new ArgumentOutOfRangeException(nameof(gameId));
            if (localUserId == 0) throw new ArgumentOutOfRangeException(nameof(localUserId));

            _gameId = gameId;
            _localUserId = localUserId;
            _diceSelectionManager = diceSelectionManager ?? throw new ArgumentNullException(
                nameof(diceSelectionManager));
            _gameplayClientProvider = gameplayClientProvider ?? throw new ArgumentNullException(
                nameof(gameplayClientProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _getIsMyTurn = getIsMyTurn ?? throw new ArgumentNullException(nameof(getIsMyTurn));
            _getIsAnimating = getIsAnimating ?? throw new ArgumentNullException(
                nameof(getIsAnimating));
            _getIsRollRequestInProgress = getIsRollRequestInProgress ??
                throw new ArgumentNullException(nameof(getIsRollRequestInProgress));
            _getIsUseItemInProgress = getIsUseItemInProgress ?? throw new ArgumentNullException(
                nameof(getIsUseItemInProgress));
            _getIsTargetSelectionActive = getIsTargetSelectionActive ??
                throw new ArgumentNullException(nameof(getIsTargetSelectionActive));
            _setIsRollRequestInProgress = setIsRollRequestInProgress ??
                throw new ArgumentNullException(nameof(setIsRollRequestInProgress));
            _raiseAllCanExecuteChanged = raiseAllCanExecuteChanged ?? throw new ArgumentNullException(
                nameof(raiseAllCanExecuteChanged));
            _syncGameStateAsync = syncGameStateAsync ?? throw new ArgumentNullException(
                nameof(syncGameStateAsync));
            _initializeInventoryAsync = initializeInventoryAsync ?? throw new ArgumentNullException(
                nameof(initializeInventoryAsync));
            _markServerEventReceived = markServerEventReceived ?? throw new ArgumentNullException(
                nameof(markServerEventReceived));
            _handleConnectionException = handleConnectionException ??
                throw new ArgumentNullException(nameof(handleConnectionException));
            _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
            _unknownErrorMessage = unknownErrorMessage ?? throw new ArgumentNullException(
                nameof(unknownErrorMessage));
            _rollDiceFailureMessagePrefix = rollDiceFailureMessagePrefix ??
                throw new ArgumentNullException(nameof(rollDiceFailureMessagePrefix));
            _rollDiceUnexpectedErrorMessage = rollDiceUnexpectedErrorMessage ?? throw new
                ArgumentNullException(nameof(rollDiceUnexpectedErrorMessage));
            _gameWindowTitle = gameWindowTitle ?? throw new ArgumentNullException(
                nameof(gameWindowTitle));
        }

        public bool CanRollDice()
        {
            _logger.InfoFormat("CanRollDice: _gameId={0}, _localUserId={1}, _isMyTurn={2}, " +
                "isAnimating={3}, _isRollRequestInProgress={4}",
                _gameId, _localUserId, _getIsMyTurn(), _getIsAnimating(),
                _getIsRollRequestInProgress());

            return PlayerActionGuard.CanRollDice(
                _getIsMyTurn(),
                _getIsAnimating(),
                _getIsRollRequestInProgress(),
                _getIsUseItemInProgress(),
                _getIsTargetSelectionActive());
        }

        public async Task RollDiceForLocalPlayerAsync()
        {
            if (_getIsRollRequestInProgress())
            {
                return;
            }

            _setIsRollRequestInProgress(true);
            _raiseAllCanExecuteChanged();

            try
            {
                IGameplayClient client = _gameplayClientProvider();
                if (client == null)
                {
                    return;
                }

                byte? diceSlotNumber = _diceSelectionManager.SelectedSlot;
                RollDiceResponseDto response = await client.GetRollDiceAsync(_gameId, _localUserId,
                    diceSlotNumber).ConfigureAwait(false);

                if (response == null || !response.Success)
                {
                    string failureReason = response != null && !string.IsNullOrWhiteSpace(response.FailureReason)
                        ? response.FailureReason
                        : _unknownErrorMessage;

                    _logger.Warn("RollDice failed: " + failureReason);
                    _showMessage(_rollDiceFailureMessagePrefix + failureReason, _gameWindowTitle,
                        MessageBoxImage.Warning);
                    return;
                }

                _markServerEventReceived();
                await ShowGrantedRewardsAsync(response, _gameWindowTitle).ConfigureAwait(false);
                _diceSelectionManager.ResetSelection();

                _logger.InfoFormat("RollDice request accepted. UserId={0}, From={1}, To={2}, Dice={3}",
                    _localUserId, response.FromCellIndex, response.ToCellIndex, response.DiceValue);

                await _syncGameStateAsync().ConfigureAwait(false);
                await _initializeInventoryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_handleConnectionException(ex, "Connection lost while rolling dice."))
                {
                    return;
                }
                _logger.Error(_rollDiceUnexpectedErrorMessage, ex);
                _showMessage(_rollDiceUnexpectedErrorMessage, _gameWindowTitle,
                    MessageBoxImage.Error);
            }
            finally
            {
                _setIsRollRequestInProgress(false);
                _raiseAllCanExecuteChanged();
            }
        }

        private static async Task ShowGrantedRewardsAsync(RollDiceResponseDto response, string gameWindowTitle)
        {
            if (string.IsNullOrWhiteSpace(response.GrantedItemCode) &&
                string.IsNullOrWhiteSpace(response.GrantedDiceCode))
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
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
                MessageBox.Show(text, gameWindowTitle, MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }
    }
}
