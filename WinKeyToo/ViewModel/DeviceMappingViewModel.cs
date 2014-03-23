using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.DirectX.DirectInput;
using WinKeyToo.AttachedCommandBehavior;
using WinKeyToo.DataAccess;
using WinKeyToo.Instrumentation;
using WinKeyToo.Internals;
using WinKeyToo.Model;

namespace WinKeyToo.ViewModel
{
    internal class DeviceMappingViewModel : WorkspaceViewModel, IDataErrorInfo
    {
        #region Fields
        readonly DeviceMapping deviceMapping;
        readonly DeviceMappingRepository deviceMappingRepository;
        public ObservableCollection<MappableDeviceInformation> Devices { get; private set; }
        public ObservableCollection<IMapActionPlugin> AvailableActions { get; private set; }
        private MappableDeviceInformation selectedDevice;
        private IMapActionPlugin selectedAction;
        public Device KeyboardDevice { get; set; }
        public MappableDeviceInformation KeyboardDeviceInfo { get; set; }
        public MappableDeviceInformation DeviceNotFoundDeviceInfo { get; set; }
        bool isSelected;
        private SimpleCommand deleteCommand;
        private SimpleCommand saveCommand;
        private SimpleCommand enterReadInputCommand;
        private SimpleCommand exitReadInputCommand;
        private SimpleCommand changeSelectionCommand;
        private SimpleCommand changeActionCommand;
        private SimpleCommand resetInputCommand;
        private readonly BackgroundWorker backgroundWorker;
        private AutoResetEvent waitEvent;

        #endregion // Fields

        #region Constructor

        public DeviceMappingViewModel()
        {
            Devices = new ObservableCollection<MappableDeviceInformation>();
            PopulateAllDevices();
            AvailableActions = new ObservableCollection<IMapActionPlugin>();
            InstantiateAvailableMapActions();
            DeviceNotFoundDeviceInfo = new MappableDeviceInformation(
                string.Format("(select a device)"));
            Devices.Add(DeviceNotFoundDeviceInfo);
            backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            backgroundWorker.RunWorkerCompleted += BackgroundWorkerRunWorkerCompleted;
            backgroundWorker.ProgressChanged += BackgroundWorkerProgressChanged;
            backgroundWorker.DoWork += BackgroundWorkerDoWork;
            deviceMapping = new DeviceMapping();
            Instructions = "Select a device";
            SelectedDevice = KeyboardDeviceInfo;
        }

        public DeviceMappingViewModel(DeviceMapping deviceMapping, DeviceMappingRepository deviceMappingRepository)
            : this()
        {
            if (deviceMapping == null)
                throw new ArgumentNullException("deviceMapping");

            if (deviceMappingRepository == null)
                throw new ArgumentNullException("deviceMappingRepository");

            this.deviceMapping = deviceMapping;
            this.deviceMappingRepository = deviceMappingRepository;
            var locateDevice = (from d in Devices where d.Instance.InstanceGuid.Equals(deviceMapping.DeviceInstanceGuid) select d).SingleOrDefault();
            if (locateDevice == null && !deviceMapping.DeviceInstanceGuid.Equals(Guid.Empty))
            {
                DeviceNotFoundDeviceInfo.Name = string.Format("({0} not found)", deviceMapping.DeviceType);
                Devices.Add(DeviceNotFoundDeviceInfo);
            }
            else
            {
                Devices.Remove(DeviceNotFoundDeviceInfo);
            }
            var locatedDevice =
                Devices.Where(d => d.Instance.InstanceGuid.Equals(deviceMapping.DeviceInstanceGuid)).SingleOrDefault() ??
                DeviceNotFoundDeviceInfo;
            SelectedDevice = locatedDevice;
            var locatedAction =
                AvailableActions.Where(a =>
                                           {
                                               var type = a.GetType();
                                               var asm = type.Assembly;
                                               return type.FullName.Equals(deviceMapping.MappingActionTypeName)
                                               && asm != null
                                               && !string.IsNullOrEmpty(asm.Location)
                                               && Path.GetFileName(asm.Location).Equals(deviceMapping.MappingActionAssemblyFileName);
                                           }).SingleOrDefault();
            SelectedAction = locatedAction;
        }

        #endregion // Constructor

        #region DeviceMapping Properties

        public DeviceMapping DeviceMapping
        {
            get { return deviceMapping; }
        }

        public bool IsEnabled
        {
            get { return deviceMapping.IsEnabled; }
            set
            {
                if (value == deviceMapping.IsEnabled) return;
                deviceMapping.IsEnabled = value;
                base.OnPropertyChanged("IsEnabled");
            }
        }

        public string SelectedDeviceName
        {
            get
            {
                return selectedDevice == null ? "" : selectedDevice.Name;
            }
        }

        public MappableDeviceInformation SelectedDevice
        {
            get { return selectedDevice; }
            set
            {
                if (value == null || value.Equals(selectedDevice)) return;
                selectedDevice = value;
                if (!deviceMapping.DeviceInstanceGuid.Equals(selectedDevice.Instance.InstanceGuid))
                {
                    deviceMapping.DeviceInstanceGuid = selectedDevice.Instance.InstanceGuid;
                    deviceMapping.DeviceType = selectedDevice.Instance.DeviceType;
                    deviceMapping.InputCombination.Clear();
                }
                Instructions = !selectedDevice.IsDevice ? "Not supported" : "Mouse over here to start binding";
                base.OnPropertyChanged("SelectedDevice");
                base.OnPropertyChanged("SelectedDeviceName");
                base.OnPropertyChanged("DisplayName");
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SelectedActionName
        {
            get
            {
                return selectedAction == null ? "" : selectedAction.DisplayName;
            }
        }

        public IMapActionPlugin SelectedAction
        {
            get { return selectedAction; }
            set
            {
                if (value == selectedAction) return;
                selectedAction = value;
                deviceMapping.SetMapActionType(selectedAction.GetType());
                SetSelectedActionConfiguration();
                base.OnPropertyChanged("SelectedAction");
                base.OnPropertyChanged("SelectedActionName");
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion // DeviceMapping Properties

        #region Presentation Properties

        private string instructions;
        public string Instructions
        {
            get { return instructions; }
            set { instructions = value; OnPropertyChanged("Instructions"); }
        }

        public override string DisplayName
        {
            get
            {
                return String.Format("{0}", deviceMapping);
            }
        }

        /// <summary>
        /// Gets/sets whether this deviceMapping is selected in the UI.
        /// </summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (value == isSelected)
                    return;

                isSelected = value;

                base.OnPropertyChanged("IsSelected");
            }
        }

        public ICommand DeleteCommand
        {
            get
            {
                if (deleteCommand == null)
                {
                    deleteCommand = new SimpleCommand { ExecuteDelegate = Delete, CanExecuteDelegate = CanDelete };
                }
                return deleteCommand;
            }
        }

        /// <summary>
        /// Returns a command that saves the deviceMapping.
        /// </summary>
        public ICommand SaveCommand
        {
            get
            {
                if (saveCommand == null)
                {
                    saveCommand = new SimpleCommand { ExecuteDelegate = Save, CanExecuteDelegate = CanSave };
                }
                return saveCommand;
            }
        }

        public ICommand ResetInputCommand
        {
            get
            {
                if (resetInputCommand == null)
                {
                    resetInputCommand = new SimpleCommand { ExecuteDelegate = ResetInput, CanExecuteDelegate = CanResetInput };
                }
                return resetInputCommand;
            }
        }

        public ICommand EnterReadInputCommand
        {
            get
            {
                if (enterReadInputCommand == null)
                {
                    enterReadInputCommand = new SimpleCommand { ExecuteDelegate = EnterReadInput, CanExecuteDelegate = CanEnterReadInput };
                }
                return enterReadInputCommand;
            }
        }

        public ICommand ExitReadInputCommand
        {
            get
            {
                if (exitReadInputCommand == null)
                {
                    exitReadInputCommand = new SimpleCommand { ExecuteDelegate = ExitReadInput, CanExecuteDelegate = CanExitReadInput };
                }
                return exitReadInputCommand;
            }
        }

        public ICommand ChangeSelectionCommand
        {
            get
            {
                if (changeSelectionCommand == null)
                {
                    changeSelectionCommand = new SimpleCommand { ExecuteDelegate = ChangeSelection, CanExecuteDelegate = CanChangeSelection };
                }
                return changeSelectionCommand;
            }
        }

        public ICommand ChangeActionCommand
        {
            get
            {
                if (changeActionCommand == null)
                {
                    changeActionCommand = new SimpleCommand { ExecuteDelegate = ChangeAction, CanExecuteDelegate = CanChangeAction };
                }
                return changeActionCommand;
            }
        }

        #endregion // Presentation Properties

        #region Public Methods

        public void ResetInput(object parameter)
        {
            using (var tracing = new Tracing())
            {
                try
                {
                    deviceMapping.InputCombination.Clear();
                    OnPropertyChanged("DisplayName");
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }

        public void ChangeSelection(object parameter)
        {
            if (parameter != null)
            {
                SelectedDevice = (MappableDeviceInformation)parameter;
            }
        }

        public void ChangeAction(object parameter)
        {
            if (parameter == null) return;
            SelectedAction = (IMapActionPlugin)parameter;
        }

        private void SetSelectedActionConfiguration()
        {
            if (SelectedAction.GetType().FullName.Equals(deviceMapping.MappingActionTypeName)
                && SelectedAction.GetType().Assembly != null
                && !string.IsNullOrEmpty(SelectedAction.GetType().Assembly.Location)
                && Path.GetFileName(SelectedAction.GetType().Assembly.Location).Equals(deviceMapping.MappingActionAssemblyFileName))
            {
                SelectedAction.Configuration = deviceMapping.MappingActionConfiguration;
            }
        }

        public void Delete(object parameter)
        {
            if (deviceMapping != null) deviceMappingRepository.DeleteDeviceMapping(deviceMapping);
        }

        /// <summary>
        /// Saves the deviceMapping to the repository.  This method is invoked by the SaveCommand.
        /// </summary>
        public void Save(object parameter)
        {
            if (!deviceMapping.IsValid)
                throw new InvalidOperationException(Strings.DeviceMappingViewModel_Exception_CannotSave);

            using (var tracing = new Tracing())
            {
                try
                {
                    deviceMapping.MappingActionConfiguration =
                        new SerializableDictionary<string, string>(selectedAction.Configuration);
                    if (IsNewDeviceMapping)
                        deviceMappingRepository.AddDeviceMapping(deviceMapping);

                    deviceMappingRepository.Save(deviceMapping.Id);
                    base.OnPropertyChanged("DisplayName");
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }

        public void EnterReadInput(object parameter)
        {
            if (SelectedDevice == null || !SelectedDevice.IsDevice || backgroundWorker.IsBusy) return;
            AttachDevice();
        }

        public void ExitReadInput(object parameter)
        {
            using (var tracing = new Tracing())
            {
                try
                {
                    if (backgroundWorker.IsBusy) backgroundWorker.CancelAsync();
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }

        public void UpdateProperties()
        {
            OnPropertyChanged("SelectedDevice");
            OnPropertyChanged("SelectedAction");
            OnPropertyChanged("DisplayName");
            OnPropertyChanged("IsEnabled");
            OnPropertyChanged("Instructions");
        }

        #endregion // Public Methods

        #region Private Helpers

        private void PopulateAllDevices()
        {
            using (var tracing = new Tracing())
            {
                Devices.Clear();
                //Populate All devices
                foreach (DeviceInstance di in Manager.Devices)
                {
                    tracing.WriteVerbose(string.Format("Located device: {0}", di.InstanceName));
                    switch (di.DeviceType)
                    {
                        case DeviceType.Keyboard:
                            KeyboardDeviceInfo = new MappableDeviceInformation(di);
                            Devices.Add(KeyboardDeviceInfo);
                            break;
                        case DeviceType.Driving:
                        case DeviceType.FirstPerson:
                        case DeviceType.Flight:
                        case DeviceType.Gamepad:
                        case DeviceType.Joystick:
                        case DeviceType.Mouse:
                        case DeviceType.Remote:
                        case DeviceType.ScreenPointer:
                            Devices.Add(new MappableDeviceInformation(di));
                            break;
                        default:
                            tracing.WriteVerbose(string.Format("Skipping {0}", di.InstanceName));
                            break;
                    }

                    ////Is device attached?
                    //TreeNode attachedNode = new TreeNode(
                    //    "Attached = " +
                    //    Manager.GetDeviceAttached(di.ProductGuid));

                    ////Get device Guid
                    //TreeNode guidNode = new TreeNode(
                    //    "Guid = " + di.InstanceGuid);

                    ////Add nodes
                    //nameNode.Nodes.Add(attachedNode);
                    //nameNode.Nodes.Add(guidNode);
                    //allNode.Nodes.Add(nameNode);
                }
            }
        }

        private void InstantiateAvailableMapActions()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            if (types.Length <= 0) return;
            foreach (var type in types)
            {
                var interfaceMapAction = (from t in type.GetInterfaces() where t.FullName == typeof(IMapActionPlugin).FullName select t).SingleOrDefault();
                if (interfaceMapAction != null)
                {
                    AvailableActions.Add((IMapActionPlugin)Activator.CreateInstance(type, true));
                }
            }
        }

        /// <summary>
        /// Returns true if this deviceMapping was created by the user and it has not yet
        /// been saved to the deviceMapping repository.
        /// </summary>
        bool IsNewDeviceMapping
        {
            get { return !deviceMappingRepository.ContainsDeviceMapping(deviceMapping); }
        }

        bool CanDelete(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Returns true if the deviceMapping is valid and can be saved.
        /// </summary>
        bool CanSave(object parameter)
        {
            return deviceMapping.IsValid;
        }

        bool CanEnterReadInput(object parameter)
        {
            return SelectedDevice != null && SelectedDevice.IsDevice && !backgroundWorker.IsBusy;
        }

        bool CanExitReadInput(object parameter)
        {
            return SelectedDevice != null && SelectedDevice.IsDevice && backgroundWorker.IsBusy;
        }

        bool CanChangeSelection(object parameter)
        {
            return parameter != null && !backgroundWorker.IsBusy;
        }

        bool CanChangeAction(object parameter)
        {
            return parameter != null;
        }

        bool CanResetInput(object parameter)
        {
            return SelectedDevice != null && SelectedDevice.IsDevice && !backgroundWorker.IsBusy && deviceMapping.InputCombination.Count > 0;
        }

        void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            using (var tracing = new Tracing())
            {
                try
                {
                    var worker = (sender as BackgroundWorker);
                    if (worker == null) return;
                    e.Result = e.Argument;
                    while (!worker.CancellationPending)
                    {
                        if (KeyboardDevice != null)
                        {
                            var pressedKeys = KeyboardDevice.GetPressedKeys();
                            if (pressedKeys != null && pressedKeys.Length > 0)
                            {
                                if (Array.IndexOf(pressedKeys, Microsoft.DirectX.DirectInput.Key.Escape) >= 0)
                                {
                                    KeyboardDevice.Unacquire();
                                    KeyboardDevice.Dispose();
                                    KeyboardDevice = null;
                                    break;
                                }
                            }
                        }
                        if (!waitEvent.WaitOne(100)) continue;
                        waitEvent.Reset();
                        worker.ReportProgress(0, e.Argument);
                    }
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }

        void BackgroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            using (var tracing = new Tracing())
            {
                try
                {
                    var worker = (sender as BackgroundWorker);
                    var device = e.UserState as Device;
                    if (device == null) return;
                    switch (device.DeviceInformation.DeviceType)
                    {
                        case DeviceType.Keyboard:
                            var pressedKeys = device.GetPressedKeys();
                            if (pressedKeys != null && pressedKeys.Length > 0)
                            {
                                if (Array.IndexOf(pressedKeys, Microsoft.DirectX.DirectInput.Key.Escape) >= 0)
                                {
                                    if (worker != null) worker.CancelAsync();
                                }
                                else
                                {
                                    foreach (var key in pressedKeys)
                                    {
                                        if (!deviceMapping.InputCombination.Contains((int)key))
                                            deviceMapping.InputCombination.Add((int)key);
                                    }
                                }
                            }
                            break;
                        case DeviceType.Mouse:
                            var buttons = device.CurrentMouseState.GetMouseButtons();
                            if (buttons != null && buttons.Length > 0)
                            {
                                for (var i = 0; i < buttons.Length; i++)
                                {
                                    if (buttons[i] != 0 && !deviceMapping.InputCombination.Contains(i))
                                        deviceMapping.InputCombination.Add(i);
                                }
                            }
                            break;
                        case DeviceType.Flight:
                        case DeviceType.Gamepad:
                        case DeviceType.Joystick:
                        case DeviceType.Remote:
                        case DeviceType.ScreenPointer:
                            buttons = device.CurrentJoystickState.GetButtons();
                            if (buttons != null && buttons.Length > 0)
                            {
                                for (var i = 0; i < buttons.Length; i++)
                                {
                                    if (buttons[i] != 0 && !deviceMapping.InputCombination.Contains(i))
                                        deviceMapping.InputCombination.Add(i);
                                }
                            }
                            break;
                        default:
                            // Mapping.Text = "Not supported";
                            break;
                    }
                    OnPropertyChanged("DisplayName");
                    // DisplayMapping(device.DeviceInformation.DeviceType);
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }

        void BackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DetachDevice(e.Result as Device);
        }

        void AttachDevice()
        {
            using (var tracing = new Tracing())
            {
                try
                {
                    KeyboardDevice = null;
                    var currentDevice = new Device(SelectedDevice.Instance.InstanceGuid);
                    tracing.WriteInfo(string.Format("Current Device: {0} - {1}",
                        currentDevice.DeviceInformation.InstanceGuid,
                        currentDevice.DeviceInformation.InstanceName));
                    if (!currentDevice.Caps.Attatched)
                    {
                        Instructions = "Sorry, that device isn't attached.";
                        return;
                    }
                    var windowHandle = new WindowInteropHelper(App.CurrentConfigurationWindow).Handle;
                    // currentDevice.SetCooperativeLevel(windowHandle, CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
                    currentDevice.SetCooperativeLevel(windowHandle,
                                                      CooperativeLevelFlags.Foreground | CooperativeLevelFlags.Exclusive);
                    waitEvent = new AutoResetEvent(false);
                    currentDevice.SetEventNotification(waitEvent);
                    currentDevice.Acquire();
                    if (SelectedDevice.Instance.DeviceType == DeviceType.Mouse)
                    {
                        Instructions = "Press ESC to exit";
                        KeyboardDevice = new Device(KeyboardDeviceInfo.Instance.InstanceGuid);
                        KeyboardDevice.SetCooperativeLevel(windowHandle,
                                                           CooperativeLevelFlags.Background |
                                                           CooperativeLevelFlags.NonExclusive);
                        KeyboardDevice.Acquire();
                    }
                    else
                    {
                        Instructions = "Move mouse out of area to stop.";
                    }
                    backgroundWorker.RunWorkerAsync(currentDevice);
                }
                catch (Microsoft.DirectX.DirectXException directXEx)
                {
                    tracing.WriteError(directXEx);
                    Instructions = "An error occurred.  Please try again.";
                }
            }
        }

        void DetachDevice(Device device)
        {
            using (var tracing = new Tracing())
            {
                try
                {
                    if (device != null)
                    {
                        device.Unacquire();
                        device.Dispose();
                    }
                    if (KeyboardDevice != null)
                    {
                        KeyboardDevice.Unacquire();
                        KeyboardDevice.Dispose();
                        KeyboardDevice = null;
                    }
                    Instructions = "Mouse here anytime to rebind";
                    CommandManager.InvalidateRequerySuggested();
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }

        #endregion // Private Helpers

        #region IDataErrorInfo Members

        string IDataErrorInfo.Error
        {
            get { return (deviceMapping as IDataErrorInfo).Error; }
        }

        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                var error = (deviceMapping as IDataErrorInfo)[propertyName];

                // Dirty the commands registered with CommandManager,
                // such as our Save command, so that they are queried
                // to see if they can execute now.
                CommandManager.InvalidateRequerySuggested();

                return error;
            }
        }
        #endregion // IDataErrorInfo Members
    }
}