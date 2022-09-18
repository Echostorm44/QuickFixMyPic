using System;
using System.Linq;
using System.Windows.Input;

namespace QuickFixMyPic.Helpers;

public class RelayCommand : ICommand
{
    readonly Action<object> execute;
    readonly Predicate<object> canExecute;

    public RelayCommand(Action<object> execute) : this(execute, null)
    {
    }

    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    {
        if(execute == null)
        {
            throw new ArgumentNullException("execute");
        }

        this.execute = execute;
        this.canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return canExecute == null ? true : canExecute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public void Execute(object parameter)
    {
        execute(parameter);
    }
}

