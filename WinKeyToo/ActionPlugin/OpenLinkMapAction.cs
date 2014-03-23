using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using WinKeyToo.Internals;

namespace WinKeyToo.ActionPlugin
{
    internal class OpenLinkMapAction : IMapActionPlugin, INotifyPropertyChanged
    {
        private readonly OpenLinkConfigurationControl configurationControl;

        public string Hyperlink
        {
            get { return Configuration["Hyperlink"]; }
            set
            {
                Configuration["Hyperlink"] = value;
                OnPropertyChanged("Hyperlink");
            }
        }

        public OpenLinkMapAction()
        {
            Configuration = new Dictionary<string, string>();
            Configuration["Hyperlink"] = string.Empty;
            configurationControl = new OpenLinkConfigurationControl {DataContext = this};
        }

        #region IMapAction Members

        public void Execute()
        {
            Process.Start("explorer.exe", Hyperlink);
        }

        private Dictionary<string, string> configuration;
        public Dictionary<string, string> Configuration
        {
            get 
            {
                if (configuration == null) configuration = new Dictionary<string, string> { { "Hyperlink", "" } };
                return configuration; 
            }
            set
            {
                if (value != null)
                {
                    configuration = value;
                }
                if (!configuration.ContainsKey("Hyperlink")) configuration.Add("Hyperlink", "");
                OnPropertyChanged("Hyperlink");
            }
        }

        public string DisplayName
        {
            get { return "Open Link"; }
        }

        public System.Windows.Controls.UserControl ConfigurationControl
        {
            get { return configurationControl; }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}