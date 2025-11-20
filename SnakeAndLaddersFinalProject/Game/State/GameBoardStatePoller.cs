using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using log4net;

namespace SnakeAndLaddersFinalProject.Game.State
{
    public sealed class GameBoardStatePoller
    {
        private const string POLL_ERROR_LOG_MESSAGE = "Error while polling game state.";

        private readonly DispatcherTimer _dispatcherTimer;
        private readonly Func<Task> _pollAction;
        private readonly ILog _logger;

        public GameBoardStatePoller(TimeSpan interval, Func<Task> pollAction, ILog logger)
        {
            if (pollAction == null)
            {
                throw new ArgumentNullException(nameof(pollAction));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _pollAction = pollAction;
            _logger = logger;

            _dispatcherTimer = new DispatcherTimer
            {
                Interval = interval
            };

            _dispatcherTimer.Tick += HandleTick;
        }

        private async void HandleTick(object sender, EventArgs args)
        {
            try
            {
                await _pollAction().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(POLL_ERROR_LOG_MESSAGE, ex);
            }
        }

        public void Start()
        {
            _dispatcherTimer.Start();
        }

        public void Stop()
        {
            _dispatcherTimer.Stop();
        }
    }
}
