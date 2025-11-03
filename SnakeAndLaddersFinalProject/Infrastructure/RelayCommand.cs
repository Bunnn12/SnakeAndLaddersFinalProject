using System;
using System.Windows.Input;

namespace SnakeAndLaddersFinalProject.Infrastructure
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        public RelayCommand(Action executeAction, Func<bool> canExecuteFunc = null)
        {
            execute = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
            canExecute = canExecuteFunc;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute();
        }

        public void Execute(object parameter)
        {
            execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
