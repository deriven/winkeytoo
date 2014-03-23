using System;
using System.Windows.Input;
using WinKeyToo.AttachedCommandBehavior;

namespace WinKeyToo.ViewModel
{
    /// <summary>
    /// This ViewModelBase subclass requests to be removed 
    /// from the UI when its CloseCommand executes.
    /// This class is abstract.
    /// </summary>
    internal abstract class WorkspaceViewModel : ViewModelBase
    {
        #region Fields

        SimpleCommand closeCommand;

        #endregion // Fields

        #region Constructor

        //protected WorkspaceViewModel()
        //{
        //}

        #endregion // Constructor

        #region CloseCommand

        /// <summary>
        /// Returns the command that, when invoked, attempts
        /// to remove this workspace from the user interface.
        /// </summary>
        public ICommand CloseCommand
        {
            get
            {
                if (closeCommand == null)
                    closeCommand = new SimpleCommand { ExecuteDelegate = OnRequestClose, CanExecuteDelegate = CanClose };

                return closeCommand;
            }
        }

        #endregion // CloseCommand

        protected virtual bool CanClose(object parameter)
        {
            return true;
        }

        #region RequestClose [event]

        /// <summary>
        /// Raised when this workspace should be removed from the UI.
        /// </summary>
        public event EventHandler RequestClose;

        protected virtual void OnRequestClose(object parameter)
        {
            var handler = RequestClose;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        #endregion // RequestClose [event]
    }
}