using System;
using System.Windows.Input;

#nullable enable

namespace WassControlSys.Core
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            // For a generic command, if the parameter is null, we check if T is a reference type or a nullable value type.
            // A simple approach is to let the _canExecute handler decide.
            if (_canExecute == null) return true;
            if (parameter == null && typeof(T).IsValueType)
            {
                // Can't execute with null on a non-nullable value type parameter
                if (Nullable.GetUnderlyingType(typeof(T)) == null) return false;
            }
            
            return parameter is T typedParam && _canExecute(typedParam);
        }

        public void Execute(object? parameter)
        {
            if (parameter is T typedParameter)
            {
                _execute(typedParameter);
            }
            // If parameter is null and T is a reference type, we might want to execute
            else if (parameter == null && !typeof(T).IsValueType)
            {
                _execute(default(T)!);
            }
        }
    }
}