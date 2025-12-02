using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SnakeAndLaddersFinalProject.Infrastructure
{
    public sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool> _canExecute;
        private bool _isRunning;

        public AsyncCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter)) return;
            _isRunning = true; RaiseCanExecuteChanged();
            try { await _executeAsync(); }
            finally { _isRunning = false; RaiseCanExecuteChanged(); }
        }

        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
