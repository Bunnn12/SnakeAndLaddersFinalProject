using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class Loading
    {
        /// <summary>
        /// Muestra Pages.LoadingPage dentro del Frame del owner mientras se ejecuta 'work'.
        /// Requiere que el Window tenga un Frame con Name="MainFrame".
        /// </summary>
        public static async Task RunOnFrameAsync(
            Window owner,
            Func<CancellationToken, Task> work,
            int minMilliseconds = 600,
            CancellationToken externalToken = default)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (work == null) throw new ArgumentNullException(nameof(work));

            // Busca el Frame principal
            var frame = owner.FindName("MainFrame") as Frame;
            if (frame == null)
                throw new InvalidOperationException("No se encontró un Frame llamado 'MainFrame' en la ventana.");

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken))
            {
                var token = cts.Token;

                // Guarda el contenido actual para restaurar si hace falta
                var previous = frame.Content;

                // Navega a la LoadingPage (cubre toda el área del Frame)
                await owner.Dispatcher.InvokeAsync(() =>
                {
                    frame.Navigate(new SnakeAndLaddersFinalProject.Pages.LoadingPage());
                });

                try
                {
                    var minDelay = Task.Delay(minMilliseconds, token);
                    var job = work(token);
                    await Task.WhenAll(job, minDelay);
                }
                finally
                {
                    // Si quieres restaurar lo anterior, descomenta:
                    // await owner.Dispatcher.InvokeAsync(() => frame.Navigate(previous));
                    // Pero normalmente aquí ya navegarás a la siguiente página/ventana.
                }
            }
        }
    }
}
