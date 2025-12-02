using System;
using System.Windows.Input;

namespace SnakeAndLaddersFinalProject.Infrastructure
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action executeAction, Func<bool> canExecuteFunc = null)
        {
            _execute = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
            _canExecute = canExecuteFunc;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
