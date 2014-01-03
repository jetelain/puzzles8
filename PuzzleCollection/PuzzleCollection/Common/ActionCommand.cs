using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PuzzleCollection.Common
{
    class ActionCommand : ICommand
    {
        private bool canExecute;
        private readonly Action execute;

        internal ActionCommand(Action execute, bool canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        internal ActionCommand(Action execute)
            : this(execute, true)
        {

        }


        public bool CanExecuteCommand
        {
            set 
            { 
                canExecute = value; 
                if (CanExecuteChanged != null) 
                { 
                    CanExecuteChanged(this, EventArgs.Empty); 
                } 
            }
            get { return canExecute; }
        }

        public bool CanExecute(object parameter)
        {
            return canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (canExecute)
            {
                execute();
            }
        }
    }
}
