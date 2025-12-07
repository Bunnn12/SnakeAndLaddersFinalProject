using log4net;
using System;
using System.Windows.Threading;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public sealed class ServerInactivityGuard : IDisposable
    {
        private const int DEFAULT_TIMEOUT_SECONDS = 45;
        private const int DEFAULT_INTERVAL_SECONDS = 5;

        private readonly int timeoutSeconds;
        private readonly DispatcherTimer timer;
        private readonly ILog logger;
        private readonly int gameId;
        private readonly int localUserId;

        private DateTime lastServerEventUtc;

        public event Action TimeoutDetected;

        public ServerInactivityGuard(
            ILog logger,
            int gameId,
            int localUserId,
            int timeoutSeconds = DEFAULT_TIMEOUT_SECONDS,
            int intervalSeconds = DEFAULT_INTERVAL_SECONDS,
            Dispatcher dispatcher = null)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameId = gameId;
            this.localUserId = localUserId;

            if (timeoutSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            }

            if (intervalSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            }

            this.timeoutSeconds = timeoutSeconds;

            Dispatcher effectiveDispatcher = dispatcher ??
                                             (System.Windows.Application.Current != null
                                                 ? System.Windows.Application.Current.Dispatcher
                                                 : Dispatcher.CurrentDispatcher);

            timer = new DispatcherTimer(
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
            lastServerEventUtc = DateTime.UtcNow;
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            double secondsWithoutEvents = (now - lastServerEventUtc).TotalSeconds;

            logger.InfoFormat(
                "ServerInactivityGuard.Tick: GameId={0}, LocalUserId={1}, SecondsWithoutEvents={2}",
                gameId,
                localUserId,
                secondsWithoutEvents);

            if (secondsWithoutEvents < timeoutSeconds)
            {
                return;
            }

            timer.Stop();

            logger.ErrorFormat(
                "ServerInactivityGuard: inactivity detected. GameId={0}, LocalUserId={1}, SecondsWithoutEvents={2}",
                gameId,
                localUserId,
                secondsWithoutEvents);

            TimeoutDetected?.Invoke();
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Tick -= OnTimerTick;
        }
    }
}
