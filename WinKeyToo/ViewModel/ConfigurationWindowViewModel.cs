using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Forms;
using Microsoft.Win32;
using WinKeyToo.AttachedCommandBehavior;
using WinKeyToo.DataAccess;

namespace WinKeyToo.ViewModel
{
    /// <summary>
    /// The ViewModel for the application's main window.
    /// </summary>
    internal class ConfigurationWindowViewModel : WorkspaceViewModel
    {
        #region Fields

        ReadOnlyCollection<CommandViewModel> commands;
        readonly DeviceMappingRepository deviceMappingRepository;
        ObservableCollection<WorkspaceViewModel> workspaces;

        #endregion // Fields

        #region Constructor

        public ConfigurationWindowViewModel(DeviceMappingRepository repository)
        {
            base.DisplayName = Strings.MainWindowViewModel_DisplayName;

            deviceMappingRepository = repository;
            ShowAllDeviceMappings(this);
        }

        #endregion // Constructor

        #region Commands

        /// <summary>
        /// Returns a read-only list of commands 
        /// that the UI can display and execute.
        /// </summary>
        public ReadOnlyCollection<CommandViewModel> Commands
        {
            get
            {
                if (commands == null)
                {
                    var cmds = CreateCommands();
                    commands = new ReadOnlyCollection<CommandViewModel>(cmds);
                }
                return commands;
            }
        }

        List<CommandViewModel> CreateCommands()
        {
            return new List<CommandViewModel>
            {
                //new CommandViewModel(
                //    Strings.MainWindowViewModel_Command_ViewAllDeviceMappings,
                //    new SimpleCommand { ExecuteDelegate = ShowAllDeviceMappings, CanExecuteDelegate = CanShowAllDeviceMappings }),
                new CommandViewModel(
                    Strings.MainWindowViewModel_Command_RunAtStartup,
                    new SimpleCommand { ExecuteDelegate = InstallRunAtStartup, CanExecuteDelegate = CanInstallRunAtStartup }),
                new CommandViewModel(
                    Strings.MainWindowViewModel_Command_DoNotRunAtStartup,
                    new SimpleCommand { ExecuteDelegate = UninstallRunAtStartup, CanExecuteDelegate = CanUninstallRunAtStartup })
                    //,
                //new CommandViewModel(
                //    Strings.MainWindowViewModel_Command_CreateNewDeviceMapping,
                //    new RelayCommand(param => CreateNewDeviceMapping()))
            };
        }

        #endregion // Commands

        #region Workspaces

        /// <summary>
        /// Returns the collection of available workspaces to display.
        /// A 'workspace' is a ViewModel that can request to be closed.
        /// </summary>
        public ObservableCollection<WorkspaceViewModel> Workspaces
        {
            get
            {
                if (workspaces == null)
                {
                    workspaces = new ObservableCollection<WorkspaceViewModel>();
                    workspaces.CollectionChanged += OnWorkspacesChanged;
                }
                return workspaces;
            }
        }

        void OnWorkspacesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.NewItems)
                    workspace.RequestClose += OnWorkspaceRequestClose;

            if (e.OldItems != null && e.OldItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.OldItems)
                    workspace.RequestClose -= OnWorkspaceRequestClose;
        }

        void OnWorkspaceRequestClose(object sender, EventArgs e)
        {
            var workspace = sender as WorkspaceViewModel;
            if (workspace == null) return;
            workspace.Dispose();
            Workspaces.Remove(workspace);
        }

        #endregion // Workspaces

        #region Private Helpers

        /*
        void CreateNewDeviceMapping()
        {
            var newDeviceMapping = DeviceMapping.CreateNewDeviceMapping();
            var workspace = new DeviceMappingViewModel(newDeviceMapping, deviceMappingRepository);
            Workspaces.Add(workspace);
            SetActiveWorkspace(workspace);
        }
*/
        bool CanShowAllDeviceMappings(object parameter)
        {
            return true;
        }

        void ShowAllDeviceMappings(object parameter)
        {
            var workspace =
                Workspaces.FirstOrDefault(vm => vm is AllDeviceMappingsViewModel)
                as AllDeviceMappingsViewModel;

            if (workspace == null)
            {
                workspace = new AllDeviceMappingsViewModel(deviceMappingRepository);
                Workspaces.Add(workspace);
            }

            SetActiveWorkspace(workspace);
        }

        private RegistryKey RunKey
        {
            get
            {
                // ReSharper disable PossibleNullReferenceException
                return Registry.CurrentUser.OpenSubKey("Software")
                    .OpenSubKey("Microsoft")
                    .OpenSubKey("Windows")
                    .OpenSubKey("CurrentVersion")
                    .OpenSubKey("Run", true);
                // ReSharper restore PossibleNullReferenceException
            }
        }

        bool CanInstallRunAtStartup(object parameter)
        {
            var runKey = RunKey;
            return 
                // !File.Exists(StartupFileName) && File.Exists(DerivenFileName);
            (runKey != null && string.IsNullOrEmpty(runKey.GetValue("WinKeyToo", string.Empty).ToString()));
        }

        void InstallRunAtStartup(object parameter)
        {
            //CreateWindowsStartup(true);
            var runKey = RunKey;
            if (runKey == null || !string.IsNullOrEmpty(runKey.GetValue("WinKeyToo", string.Empty).ToString())) return;
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()
                                                 .GetName()
                                                 .CodeBase);
            runKey.SetValue("WinKeyToo", path);
        }

        bool CanUninstallRunAtStartup(object parameter)
        {
            //return File.Exists(StartupFileName);
            var runKey = RunKey;
            return
                (runKey != null && !string.IsNullOrEmpty(runKey.GetValue("WinKeyToo", string.Empty).ToString()));
        }

        void UninstallRunAtStartup(object parameter)
        {
            //CreateWindowsStartup(false);
            var runKey = RunKey;
            if (runKey == null || string.IsNullOrEmpty(runKey.GetValue("WinKeyToo", string.Empty).ToString())) return;
            runKey.DeleteValue("WinKeyToo", false);
        }

        private static string StartupFileName
        {
            get
            {
                return Environment.GetFolderPath(System.Environment.SpecialFolder.Startup)
                       + "\\" + Application.ProductName + ".appref-ms";
            }
        }

        private static string DerivenFileName
        {
            get
            {
                return Environment.GetFolderPath(System.Environment.SpecialFolder.Programs)
                       + "\\Deriven\\" + Application.ProductName + ".appref-ms";
            }
        }

        internal static void CreateWindowsStartup(bool create) 
        {
            if (create) 
            { 
                if (!File.Exists(StartupFileName) && File.Exists(DerivenFileName)) 
                {
                    File.Copy(DerivenFileName, StartupFileName, true);
                } 
            } 
            else 
            { 
                if (File.Exists(StartupFileName)) 
                { 
                    File.Delete(StartupFileName); 
                } 
            } 
        }

        void SetActiveWorkspace(WorkspaceViewModel workspace)
        {
            Debug.Assert(Workspaces.Contains(workspace));

            var collectionView = CollectionViewSource.GetDefaultView(Workspaces);
            if (collectionView != null)
                collectionView.MoveCurrentTo(workspace);
        }

        #endregion // Private Helpers
    }
}