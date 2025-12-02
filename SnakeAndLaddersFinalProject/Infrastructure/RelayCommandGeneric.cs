using System;
using System.Windows.Input;

namespace SnakeAndLaddersFinalProject.Infrastructure
{
    public sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> executeAction, Func<T, bool> canExecuteFunc = null)
        {
            _execute = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
            _canExecute = canExecuteFunc;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            T converted = ConvertParameter(parameter);
            return _canExecute(converted);
        }

        public void Execute(object parameter)
        {
            T converted = ConvertParameter(parameter);
            _execute(converted);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        private static T ConvertParameter(object parameter)
        {
            if (parameter == null)
            {
                return default;
            }

            if (parameter is T value)
            {
                return value;
            }

            try
            {
                return (T)Convert.ChangeType(parameter, typeof(T));
            }
            catch
            {
                return default;
            }
        }
    }
}
