using System;
using System.Windows.Input;

namespace SnakeAndLaddersFinalProject
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _executeAction;
        private readonly Predicate<object> _canExecutePredicate;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _executeAction = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecutePredicate = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecutePredicate == null)
            {
                return true;
            }

            return _canExecutePredicate.Invoke(parameter);
        }

        public void Execute(object parameter)
        {
            _executeAction(parameter);
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
