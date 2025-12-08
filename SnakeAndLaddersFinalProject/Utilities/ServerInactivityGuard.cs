using log4net;
using System;
using System.Windows.Threading;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public sealed class ServerInactivityGuard : IDisposable
    {
        private const int DEFAULT_TIMEOUT_SECONDS = 45;
        private const int DEFAULT_INTERVAL_SECONDS = 5;

        private readonly int _timeoutSeconds;
        private readonly DispatcherTimer _timer;
        private readonly ILog _logger;
        private readonly int _gameId;
        private readonly int _localUserId;

        private DateTime _lastServerEventUtc;

        public event Action ServerInactivityTimeoutDetected;

        public ServerInactivityGuard(
            ILog logger,
            int gameId,
            int localUserId,
            int timeoutSeconds = DEFAULT_TIMEOUT_SECONDS,
            int intervalSeconds = DEFAULT_INTERVAL_SECONDS,
            Dispatcher dispatcher = null)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._gameId = gameId;
            this._localUserId = localUserId;

            if (timeoutSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            }

            if (intervalSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            }

            this._timeoutSeconds = timeoutSeconds;

            Dispatcher effectiveDispatcher = dispatcher ??
                                             (System.Windows.Application.Current != null
                                                 ? System.Windows.Application.Current.Dispatcher
                                                 : Dispatcher.CurrentDispatcher);

            _timer = new DispatcherTimer(
                TimeSpan.FromSeconds(intervalSeconds),
                DispatcherPriority.Background,
                OnTimerTick,
                effectiveDispatcher);

            MarkServerEventReceived();

            logger.InfoFormat(
                "ServerInactivityGuard creado. GameId={0}, LocalUserId={1}, Timeout={2}s, Interval={3}s, DispatcherThreadId={4}.",
                gameId,
                localUserId,
                timeoutSeconds,
                intervalSeconds,
                effectiveDispatcher.Thread.ManagedThreadId);
        }

        public void MarkServerEventReceived()
        {
            _lastServerEventUtc = DateTime.UtcNow;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            double secondsWithoutEvents = (now - _lastServerEventUtc).TotalSeconds;

            _logger.InfoFormat(
                "ServerInactivityGuard.Tick: GameId={0}, LocalUserId={1}, SecondsWithoutEvents={2}",
                _gameId,
                _localUserId,
                secondsWithoutEvents);

            if (secondsWithoutEvents < _timeoutSeconds)
            {
                return;
            }

            _timer.Stop();

            _logger.ErrorFormat(
                "ServerInactivityGuard: inactivity detected. GameId={0}, LocalUserId={1}, SecondsWithoutEvents={2}",
                _gameId,
                _localUserId,
                secondsWithoutEvents);

            ServerInactivityTimeoutDetected?.Invoke();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
        }
    }
}
