using System;
using System.Windows.Input;

namespace WinKeyToo.ViewModel
{
    /// <summary>
    /// Represents an actionable item displayed by a View.
    /// </summary>
    internal class CommandViewModel : ViewModelBase
    {
        public CommandViewModel(string displayName, ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            base.DisplayName = displayName;
            Command = command;
        }

        public ICommand Command { get; private set; }
    }
}