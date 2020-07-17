using System;
using System.Windows.Input;

namespace BaseWPFLibrary.Bindings
{
    public abstract class UpdateableCommand : ICommand
    {
        private bool _validState = true;
        public event EventHandler CanExecuteChanged;
        public Func<bool> ValidStateFunction = null;

        /// <summary>
        /// Specifies whether the state of the command is in a valid state.
        /// Note, if the ValidStateFunction is given, then the result will *always* be given by this function. I.e. you cannot set the value of this property manually.
        /// </summary>
        public virtual bool ValidState
        {
            get
            {
                if (ValidStateFunction != null) return ValidStateFunction();
                else return _validState;
            }
            set
            {
                if (ValidStateFunction != null) throw new InvalidOperationException("The ValidStateFunction is set, thus you cannot set the ValidState property manually.");
                
                bool changed = _validState != value;
                _validState = value;
                if (changed) OnCanExecuteChanged();
            }
        }

        public virtual bool CanExecute(object parameter)
        {
            if (ValidStateFunction != null) return ValidStateFunction();
            else return _validState;
        }

        public abstract void Execute(object parameter);

        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }
}
