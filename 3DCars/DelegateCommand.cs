
using System;
using System.Windows.Input;

namespace _3DCars
{
    public class DelegateCommand : ICommand
    {
        private Action execute;

        public event EventHandler CanExecuteChanged;

        private void OnCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public DelegateCommand(Action execute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        void ICommand.Execute(object parameter)
        {
            this.execute();
        }
    }

}