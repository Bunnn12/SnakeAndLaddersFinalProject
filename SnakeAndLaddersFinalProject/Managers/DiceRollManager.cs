using System;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Services;
using SnakeAndLaddersFinalProject.Utilities;

namespace SnakeAndLaddersFinalProject.Managers
{
    public sealed class DiceRollManager
    {
        private const int MIN_GAME_ID = 1;
        private const int INVALID_USER_ID = 0;

        private const string LOG_CAN_ROLL_DICE_FORMAT =
            "CanRollDice: GameId={0}, LocalUserId={1}, IsMyTurn={2}, " +
            "IsAnimating={3}, IsRollRequestInProgress={4}";

        private const string LOG_ROLL_DICE_FAILED_PREFIX = "RollDice failed: ";

        private const string LOG_ROLL_DICE_ACCEPTED_FORMAT =
            "RollDice request accepted. UserId={0}, From={1}, To={2}, Dice={3}";

        private const string CONNECTION_LOST_WHILE_ROLLING_MESSAGE =
            "Connection lost while rolling dice.";

        private const string REWARD_MESSAGE_PREFIX = "You have obtained ";
        private const string REWARD_ITEM_FORMAT = "an item ({0})";
        private const string REWARD_DICE_FORMAT = "a dice ({0})";
        private const string REWARD_CONJUNCTION = " y ";
        private const string REWARD_MESSAGE_SUFFIX = "!";

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
            DiceRollManagerDependencies dependencies,
            DiceRollMessages messages)
        {
            if (gameId < MIN_GAME_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            if (localUserId <= INVALID_USER_ID)
            {
                throw new ArgumentOutOfRangeException(nameof(localUserId));
            }

            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            _gameId = gameId;
            _localUserId = localUserId;

            _diceSelectionManager = dependencies.DiceSelectionManager
                ?? throw new ArgumentNullException(nameof(dependencies));

            _gameplayClientProvider = dependencies.GameplayClientProvider
                ?? throw new ArgumentNullException(nameof(dependencies));

            _logger = dependencies.Logger
                ?? throw new ArgumentNullException(nameof(dependencies));

            _getIsMyTurn = dependencies.GetIsMyTurn
                ?? throw new ArgumentNullException(nameof(dependencies));

            _getIsAnimating = dependencies.GetIsAnimating
                ?? throw new ArgumentNullException(nameof(dependencies));

            _getIsRollRequestInProgress = dependencies.GetIsRollRequestInProgress
                ?? throw new ArgumentNullException(nameof(dependencies));

            _getIsUseItemInProgress = dependencies.GetIsUseItemInProgress
                ?? throw new ArgumentNullException(nameof(dependencies));

            _getIsTargetSelectionActive = dependencies.GetIsTargetSelectionActive
                ?? throw new ArgumentNullException(nameof(dependencies));

            _setIsRollRequestInProgress = dependencies.SetIsRollRequestInProgress
                ?? throw new ArgumentNullException(nameof(dependencies));

            _raiseAllCanExecuteChanged = dependencies.RaiseAllCanExecuteChanged
                ?? throw new ArgumentNullException(nameof(dependencies));

            _syncGameStateAsync = dependencies.SyncGameStateAsync
                ?? throw new ArgumentNullException(nameof(dependencies));

            _initializeInventoryAsync = dependencies.InitializeInventoryAsync
                ?? throw new ArgumentNullException(nameof(dependencies));

            _markServerEventReceived = dependencies.MarkServerEventReceived
                ?? throw new ArgumentNullException(nameof(dependencies));

            _handleConnectionException = dependencies.HandleConnectionException
                ?? throw new ArgumentNullException(nameof(dependencies));

            _showMessage = dependencies.ShowMessage
                ?? throw new ArgumentNullException(nameof(dependencies));

            _unknownErrorMessage = messages.UnknownErrorMessage
                ?? throw new ArgumentNullException(nameof(messages));

            _rollDiceFailureMessagePrefix = messages.RollDiceFailureMessagePrefix
                ?? throw new ArgumentNullException(nameof(messages));

            _rollDiceUnexpectedErrorMessage = messages.RollDiceUnexpectedErrorMessage
                ?? throw new ArgumentNullException(nameof(messages));

            _gameWindowTitle = messages.GameWindowTitle
                ?? throw new ArgumentNullException(nameof(messages));
        }

        public bool CanRollDice()
        {
            bool isMyTurn = _getIsMyTurn();
            bool isAnimating = _getIsAnimating();
            bool isRollRequestInProgress = _getIsRollRequestInProgress();

            _logger.InfoFormat(
                LOG_CAN_ROLL_DICE_FORMAT,
                _gameId,
                _localUserId,
                isMyTurn,
                isAnimating,
                isRollRequestInProgress);

            return PlayerActionGuard.CanRollDice(
                isMyTurn,
                isAnimating,
                isRollRequestInProgress,
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

                RollDiceResponseDto response = await client
                    .GetRollDiceAsync(_gameId, _localUserId, diceSlotNumber)
                    .ConfigureAwait(false);

                if (!IsSuccessfulResponse(response))
                {
                    HandleRollDiceFailure(response);
                    return;
                }

                _markServerEventReceived();

                await ShowGrantedRewardsAsync(response, _gameWindowTitle)
                    .ConfigureAwait(false);

                _diceSelectionManager.ResetSelection();

                _logger.InfoFormat(
                    LOG_ROLL_DICE_ACCEPTED_FORMAT,
                    _localUserId,
                    response.FromCellIndex,
                    response.ToCellIndex,
                    response.DiceValue);

                await _syncGameStateAsync().ConfigureAwait(false);
                await _initializeInventoryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_handleConnectionException(
                    ex,
                    CONNECTION_LOST_WHILE_ROLLING_MESSAGE))
                {
                    return;
                }

                _logger.Error(_rollDiceUnexpectedErrorMessage, ex);

                _showMessage(
                    _rollDiceUnexpectedErrorMessage,
                    _gameWindowTitle,
                    MessageBoxImage.Error);
            }
            finally
            {
                _setIsRollRequestInProgress(false);
                _raiseAllCanExecuteChanged();
            }
        }

        private static bool IsSuccessfulResponse(RollDiceResponseDto response)
        {
            return response != null && response.Success;
        }

        private void HandleRollDiceFailure(RollDiceResponseDto response)
        {
            string failureReason = GetFailureReason(response);

            _logger.Warn(LOG_ROLL_DICE_FAILED_PREFIX + failureReason);

            string message = _rollDiceFailureMessagePrefix + failureReason;

            _showMessage(
                message,
                _gameWindowTitle,
                MessageBoxImage.Warning);
        }

        private string GetFailureReason(RollDiceResponseDto response)
        {
            if (response != null &&
                !string.IsNullOrWhiteSpace(response.FailureReason))
            {
                return response.FailureReason;
            }

            return _unknownErrorMessage;
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
                    string messageText = BuildRewardMessageText(response);

                    MessageBox.Show(
                        messageText,
                        gameWindowTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
        }

        private static string BuildRewardMessageText(RollDiceResponseDto response)
        {
            string message = REWARD_MESSAGE_PREFIX;
            bool hasItem = !string.IsNullOrWhiteSpace(response.GrantedItemCode);
            bool hasDice = !string.IsNullOrWhiteSpace(response.GrantedDiceCode);

            if (hasItem)
            {
                message += string.Format(
                    REWARD_ITEM_FORMAT,
                    response.GrantedItemCode);
            }

            if (hasDice)
            {
                if (hasItem)
                {
                    message += REWARD_CONJUNCTION;
                }

                message += string.Format(
                    REWARD_DICE_FORMAT,
                    response.GrantedDiceCode);
            }

            message += REWARD_MESSAGE_SUFFIX;

            return message;
        }
    }

    public sealed class DiceRollManagerDependencies
    {
        public DiceSelectionManager DiceSelectionManager { get; set; }
        public Func<IGameplayClient> GameplayClientProvider { get; set; }
        public ILog Logger { get; set; }

        public Func<bool> GetIsMyTurn { get; set; }
        public Func<bool> GetIsAnimating { get; set; }
        public Func<bool> GetIsRollRequestInProgress { get; set; }
        public Func<bool> GetIsUseItemInProgress { get; set; }
        public Func<bool> GetIsTargetSelectionActive { get; set; }

        public Action<bool> SetIsRollRequestInProgress { get; set; }
        public Action RaiseAllCanExecuteChanged { get; set; }

        public Func<Task> SyncGameStateAsync { get; set; }
        public Func<Task> InitializeInventoryAsync { get; set; }

        public Action MarkServerEventReceived { get; set; }
        public Func<Exception, string, bool> HandleConnectionException { get; set; }
        public Action<string, string, MessageBoxImage> ShowMessage { get; set; }
    }

    public sealed class DiceRollMessages
    {
        public string UnknownErrorMessage { get; set; }
        public string RollDiceFailureMessagePrefix { get; set; }
        public string RollDiceUnexpectedErrorMessage { get; set; }
        public string GameWindowTitle { get; set; }
    }
}
