using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.Animation;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Game.Gameplay
{
    public sealed class GameplayEventsHandler
    {
        private const string PLAYER_MOVED_ERROR_LOG_MESSAGE = "Error al procesar movimiento " +
            "desde el servidor.";
        private const string TURN_CHANGED_ERROR_LOG_MESSAGE = "Error al procesar cambio de turno.";
        private const string PLAYER_LEFT_ERROR_LOG_MESSAGE = "Error al procesar PlayerLeft.";
        private const int DEFAULT_DICE_FACE_FOR_ANIMATION = 1;

        private readonly GameBoardAnimationService _animationService;
        private readonly DiceSpriteAnimator _diceSpriteAnimator;
        private readonly AsyncCommand _rollDiceCommand;
        private readonly ILog _logger;
        private readonly int _localUserId;
        private readonly Action<int> _updateTurnFromState;

        public GameplayEventsHandler(GameBoardAnimationService animationService,
            DiceSpriteAnimator diceSpriteAnimator, AsyncCommand rollDiceCommand,
            ILog logger, int localUserId, Action<int> updateTurnFromState)
        {
            _animationService = animationService ?? throw new ArgumentNullException(
                nameof(animationService));
            _diceSpriteAnimator = diceSpriteAnimator ?? throw new ArgumentNullException(
                nameof(diceSpriteAnimator));
            _rollDiceCommand = rollDiceCommand ?? throw new ArgumentNullException(
                nameof(rollDiceCommand));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updateTurnFromState = updateTurnFromState ?? throw new ArgumentNullException(
                nameof(updateTurnFromState));
            _localUserId = localUserId;
        }

        public Task HandleServerPlayerMovedAsync(PlayerMoveResultDto move)
        {
            if (move == null)
            {
                return Task.CompletedTask;
            }
            return HandlePlayerMovedInternalAsync(move);
        }

        private async Task HandlePlayerMovedInternalAsync(PlayerMoveResultDto move)
        {
            try
            {
                int userId = move.UserId;
                int fromIndex = move.FromCellIndex;
                int toIndex = move.ToCellIndex;
                int diceValue = move.DiceValue;

                int diceFaceForAnimation = Math.Abs(diceValue);
                if (diceFaceForAnimation <= 0)
                {
                    diceFaceForAnimation = DEFAULT_DICE_FACE_FOR_ANIMATION;
                }

                await _diceSpriteAnimator.PlayRollAnimationAsync(diceFaceForAnimation).
                    ConfigureAwait(false);
                await _animationService.AnimateMoveForLocalPlayerAsync(userId, fromIndex,
                    toIndex, diceValue).
                    ConfigureAwait(false);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _rollDiceCommand.RaiseCanExecuteChanged();

                    if (userId == _localUserId)
                    {
                        string message;
                        if (diceValue > 0)
                        {
                            message = string.Format(Lang.GameDiceMoveForwardFmt, diceValue,
                                fromIndex, toIndex);
                        }
                        else if (diceValue < 0)
                        {
                            int steps = Math.Abs(diceValue);
                            message = string.Format(Lang.GameDiceMoveBackwardFmt, diceValue,
                                fromIndex, toIndex, steps);
                        }
                        else
                        {
                            message = string.Format(Lang.GameDiceNoMovementFmt, fromIndex);
                        }
                        MessageBox.Show(message, Lang.GameDiceResultTitle, MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }, DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                _logger.Error(PLAYER_MOVED_ERROR_LOG_MESSAGE, ex);
            }
        }

        public Task HandleServerTurnChangedAsync(TurnChangedDto turnInfo)
        {
            if (turnInfo == null)
            {
                return Task.CompletedTask;
            }

            try
            {
                _updateTurnFromState(turnInfo.CurrentTurnUserId);
            }
            catch (Exception ex)
            {
                _logger.Error(TURN_CHANGED_ERROR_LOG_MESSAGE, ex);
            }
            return Task.CompletedTask;
        }

        public Task HandleServerPlayerLeftAsync(PlayerLeftDto playerLeftInfo)
        {
            if (playerLeftInfo == null)
            {
                return Task.CompletedTask;
            }

            try
            {
                if (playerLeftInfo.UserId != _localUserId)
                {
                    string userName = string.IsNullOrWhiteSpace(playerLeftInfo.UserName)
                        ? string.Format(Lang.GameLeftPlayerFallbackNameFmt, playerLeftInfo.UserId)
                        : playerLeftInfo.UserName;

                    string message = string.Format(Lang.GameLeftPlayerMessageFmt, userName);
                    MessageBox.Show(message, Lang.GamePlayerLeftTitle, MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                if (playerLeftInfo.NewCurrentTurnUserId.HasValue)
                {
                    _updateTurnFromState(playerLeftInfo.NewCurrentTurnUserId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(PLAYER_LEFT_ERROR_LOG_MESSAGE, ex);
            }
            return Task.CompletedTask;
        }
    }
}
