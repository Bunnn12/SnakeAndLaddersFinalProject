using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using log4net;
using SnakeAndLaddersFinalProject.Animation;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Infrastructure;

namespace SnakeAndLaddersFinalProject.Game.Gameplay
{
    public sealed class GameplayEventsHandler
    {
        private const string DICE_RESULT_TITLE = "Resultado del dado";
        private const string PLAYER_LEFT_TITLE = "Jugador salió";
        private const string PLAYER_MOVED_ERROR_LOG_MESSAGE = "Error al procesar movimiento desde el servidor.";
        private const string TURN_CHANGED_ERROR_LOG_MESSAGE = "Error al procesar cambio de turno.";
        private const string PLAYER_LEFT_ERROR_LOG_MESSAGE = "Error al procesar PlayerLeft.";

        private readonly GameBoardAnimationService _animationService;
        private readonly DiceSpriteAnimator _diceSpriteAnimator;
        private readonly AsyncCommand _rollDiceCommand;
        private readonly ILog _logger;
        private readonly int _localUserId;
        private readonly Action<int> _updateTurnFromState;

        public GameplayEventsHandler(
            GameBoardAnimationService animationService,
            DiceSpriteAnimator diceSpriteAnimator,
            AsyncCommand rollDiceCommand,
            ILog logger,
            int localUserId,
            Action<int> updateTurnFromState)
        {
            if (animationService == null)
            {
                throw new ArgumentNullException(nameof(animationService));
            }

            if (diceSpriteAnimator == null)
            {
                throw new ArgumentNullException(nameof(diceSpriteAnimator));
            }

            if (rollDiceCommand == null)
            {
                throw new ArgumentNullException(nameof(rollDiceCommand));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (updateTurnFromState == null)
            {
                throw new ArgumentNullException(nameof(updateTurnFromState));
            }

            _animationService = animationService;
            _diceSpriteAnimator = diceSpriteAnimator;
            _rollDiceCommand = rollDiceCommand;
            _logger = logger;
            _localUserId = localUserId;
            _updateTurnFromState = updateTurnFromState;
        }

        public Task HandleServerPlayerMovedAsync(PlayerMoveResultDto move)
        {
            if (move == null)
            {
                return Task.CompletedTask;
            }

            Task handlerTask = HandlePlayerMovedInternalAsync(move);
            return handlerTask;
        }

        private async Task HandlePlayerMovedInternalAsync(PlayerMoveResultDto move)
        {
            try
            {
                int userId = move.UserId;
                int fromIndex = move.FromCellIndex;
                int toIndex = move.ToCellIndex;
                int diceValue = move.DiceValue;

                // 1) Animar dado (fuera del Dispatcher, internamente ya usará el hilo de UI si lo necesita)
                await _diceSpriteAnimator
                    .RollAsync(diceValue)
                    .ConfigureAwait(false);

                // 2) Animar movimiento del jugador
                await _animationService
                    .AnimateMoveForLocalPlayerAsync(userId, fromIndex, toIndex, diceValue)
                    .ConfigureAwait(false);

                // 3) Actualizar command y mostrar mensaje en el hilo de UI
                await Application.Current.Dispatcher.InvokeAsync(
                    () =>
                    {
                        _rollDiceCommand.RaiseCanExecuteChanged();

                        if (userId == _localUserId)
                        {
                            string message = string.Format(
                                "Sacaste {0} y avanzaste de {1} a {2}.",
                                diceValue,
                                fromIndex,
                                toIndex);

                            MessageBox.Show(
                                message,
                                DICE_RESULT_TITLE,
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    },
                    DispatcherPriority.Normal);
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
                        ? string.Format("Jugador {0}", playerLeftInfo.UserId)
                        : playerLeftInfo.UserName;

                    string message = string.Format("{0} abandonó la partida.", userName);

                    MessageBox.Show(
                        message,
                        PLAYER_LEFT_TITLE,
                        MessageBoxButton.OK,
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
