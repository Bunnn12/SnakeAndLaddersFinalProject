using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using log4net;
using log4net.Repository.Hierarchy;
using SnakeAndLaddersFinalProject.Pages;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class Loading
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Loading));
        private const int DEFAULT_MIN_LOADING_MILLISECONDS = 600;
        public static async Task RunOnFrameAsync(
            Window owner,
            Func<CancellationToken, Task> work,
            int minMilliseconds = DEFAULT_MIN_LOADING_MILLISECONDS,
            CancellationToken externalToken = default)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (work == null) throw new ArgumentNullException(nameof(work));

            Frame mainFrame = owner.FindName("MainFrame") as Frame;
            if (mainFrame == null)
                throw new InvalidOperationException(Lang.LoadingMainFrameNotFoundError);

            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken))
            {
                CancellationToken cancellationToken = linkedCts.Token;

                object previous = mainFrame.Content;

                await owner.Dispatcher.InvokeAsync(() =>
                {
                    mainFrame.Navigate(new SnakeAndLaddersFinalProject.Pages.LoadingPage());
                });

                try
                {
                    Task minimumDelayTask = Task.Delay(minMilliseconds, cancellationToken);
                    Task jobTask = work(cancellationToken);
                    await Task.WhenAll(jobTask, minimumDelayTask);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.Warn("Loading.RunOnFrameAsync was canceled.", ex);
                }
                catch (Exception ex)
                {
                    _logger.Error("Unexpected error while running Loading.RunOnFrameAsync.", ex);
                }
            }
        }
    }
}
