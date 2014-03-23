using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using WinKeyToo.AttachedCommandBehavior;
using WinKeyToo.Internals;

namespace WinKeyToo.ActionPlugin
{
    internal class LaunchApplicationMapAction : IMapActionPlugin, INotifyPropertyChanged
    {
        private readonly LaunchApplicationConfigurationControl configurationControl;
        private ICommand browseFileCommand;
        private ICommand browseFolderCommand;

        public string ApplicationPath
        {
            get { return Configuration["ApplicationPath"]; }
            set
            {
                Configuration["ApplicationPath"] = value;
                OnPropertyChanged("ApplicationPath");
            }
        }

        public string CommandLineArguments
        {
            get { return Configuration["CommandLineArguments"]; }
            set
            {
                Configuration["CommandLineArguments"] = value;
                OnPropertyChanged("CommandLineArguments");
            }
        }

        public string StartInFolder
        {
            get { return Configuration["StartInFolder"]; }
            set
            {
                Configuration["StartInFolder"] = value;
                OnPropertyChanged("StartInFolder");
            }
        }

        public ICommand BrowseFileCommand
        {
            get
            {
                if (browseFileCommand == null)
                {
                    browseFileCommand = new SimpleCommand { ExecuteDelegate = BrowseFile, CanExecuteDelegate = CanBrowseFile };
                }
                return browseFileCommand;
            }
        }

        public void BrowseFile(object parameter)
        {
            var ofd = new OpenFileDialog
            {
                InitialDirectory = string.IsNullOrEmpty(ApplicationPath) ?
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : ApplicationPath
            };
            var result = ofd.ShowDialog(configurationControl.GetIWin32Window());
            if (result == DialogResult.OK
                && !string.IsNullOrEmpty(ofd.FileName)
                && File.Exists(ofd.FileName))
            {
                ApplicationPath = ofd.FileName;
            }
        }

        public bool CanBrowseFile(object parameter)
        {
            return true;
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
                SelectedPath = string.IsNullOrEmpty(StartInFolder) ?
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : StartInFolder,
                ShowNewFolderButton = true
            };
            var result = fbd.ShowDialog(configurationControl.GetIWin32Window());
            if (result == DialogResult.OK
                && !string.IsNullOrEmpty(fbd.SelectedPath)
                && Directory.Exists(fbd.SelectedPath))
            {
                StartInFolder = fbd.SelectedPath;
            }
        }

        public bool CanBrowseFolder(object parameter)
        {
            return true;
        }

        public LaunchApplicationMapAction()
        {
            Configuration = new Dictionary<string, string>();
            Configuration["ApplicationPath"] = string.Empty;
            Configuration["CommandLineArguments"] = string.Empty;
            Configuration["StartInFolder"] = string.Empty;
            configurationControl = new LaunchApplicationConfigurationControl { DataContext = this };
        }

        #region IMapAction Members

        public void Execute()
        {
            if (!File.Exists(ApplicationPath)) return;
            var processInfo = new ProcessStartInfo(ApplicationPath, CommandLineArguments);
            if (Directory.Exists(StartInFolder)) processInfo.WorkingDirectory = StartInFolder;
            Process.Start(processInfo);
            //Trace.WriteLine("Executed action");
        }

        private Dictionary<string, string> configuration;
        public Dictionary<string, string> Configuration
        {
            get 
            {
                if (configuration == null) configuration = new Dictionary<string, string> 
                { 
                    { "ApplicationPath", "" }, 
                    { "StartInFolder", "" },
                    { "CommandLineArguments", "" } 
                };
                return configuration; 
            }
            set
            {
                if (value != null)
                {
                    configuration = value;
                }
                if (!configuration.ContainsKey("ApplicationPath")) configuration.Add("ApplicationPath", "");
                OnPropertyChanged("ApplicationPath");
                if (!configuration.ContainsKey("StartInFolder")) configuration.Add("StartInFolder", "");
                OnPropertyChanged("StartInFolder");
                if (!configuration.ContainsKey("CommandLineArguments")) configuration.Add("CommandLineArguments", "");
                OnPropertyChanged("CommandLineArguments");
            }
        }

        public string DisplayName
        {
            get { return "Launch Application"; }
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