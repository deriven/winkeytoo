using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using WinKeyToo.AttachedCommandBehavior;
using WinKeyToo.Internals;

namespace WinKeyToo.ActionPlugin
{
    internal class OpenFolderMapAction : IMapActionPlugin, INotifyPropertyChanged
    {
        private readonly OpenFolderConfigurationControl configurationControl;
        private ICommand browseFolderCommand;

        public string FolderPath
        {
            get { return Configuration["FolderPath"]; }
            set
            {
                Configuration["FolderPath"] = value;
                OnPropertyChanged("FolderPath");
            }
        }

        public ICommand BrowseFolderCommand
        {
            get
            {
                if (browseFolderCommand == null)
                {
                    browseFolderCommand = new SimpleCommand { ExecuteDelegate = BrowseFolder, CanExecuteDelegate = CanBrowseFolder };
                }
                return browseFolderCommand;
            }
        }

        public void BrowseFolder(object parameter)
        {
            var fbd = new FolderBrowserDialog
            {
                SelectedPath = string.IsNullOrEmpty(FolderPath) ?
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : FolderPath,
                ShowNewFolderButton = true
            };
            var result = fbd.ShowDialog(configurationControl.GetIWin32Window());
            if (result == DialogResult.OK
                && !string.IsNullOrEmpty(fbd.SelectedPath)
                && Directory.Exists(fbd.SelectedPath))
            {
                FolderPath = fbd.SelectedPath;
            }
        }

        public bool CanBrowseFolder(object parameter)
        {
            return true;
        }

        public OpenFolderMapAction()
        {
            Configuration = new Dictionary<string, string>();
            Configuration["FolderPath"] = string.Empty;
            configurationControl = new OpenFolderConfigurationControl {DataContext = this};
        }

        #region IMapAction Members

        public void Execute()
        {
            if (Directory.Exists(FolderPath))
            {
                Process.Start("explorer.exe", FolderPath);
            }
        }

        private Dictionary<string, string> configuration;
        public Dictionary<string, string> Configuration
        {
            get 
            {
                if (configuration == null) configuration = new Dictionary<string, string> { { "FolderPath", "" } };
                return configuration; 
            }
            set
            {
                if (value != null)
                {
                    configuration = value;
                }
                if (!configuration.ContainsKey("FolderPath")) configuration.Add("FolderPath", "");
                OnPropertyChanged("FolderPath");
            }
        }

        public string DisplayName
        {
            get { return "Open Folder"; }
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