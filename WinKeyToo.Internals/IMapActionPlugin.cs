using System.Collections.Generic;
using System.Windows.Controls;

namespace WinKeyToo.Internals
{
    public interface IMapActionPlugin
    {
        string DisplayName { get; }
        Dictionary<string, string> Configuration { get; set; }
        UserControl ConfigurationControl { get; }
        void Execute();
    }
}