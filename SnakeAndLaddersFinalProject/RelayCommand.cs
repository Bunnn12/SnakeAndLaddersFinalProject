using System;
using System.Windows.Input;

namespace SnakeAndLaddersFinalProject
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> executeAction;
        private readonly Predicate<object> canExecutePredicate;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            executeAction = execute ?? throw new ArgumentNullException(nameof(execute));
            canExecutePredicate = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (canExecutePredicate == null)
            {
                return true;
            }

            return canExecutePredicate.Invoke(parameter);
        }

        public void Execute(object parameter)
        {
            executeAction(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }
    }
}
