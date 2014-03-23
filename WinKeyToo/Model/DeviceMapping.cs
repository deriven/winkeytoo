using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Microsoft.DirectX.DirectInput;

namespace WinKeyToo.Model
{
    public class DeviceMapping : IEquatable<DeviceMapping>, IDataErrorInfo
    {
        #region "Private Variables"

        private DeviceType deviceType = DeviceType.Device;
        private Guid deviceInstanceGuid = Guid.Empty;
        private List<int> inputCombination = new List<int>();

        //private AutoResetEvent notifier;
        //private readonly BackgroundWorker monitorWorker;
        #endregion

        #region "Public Properties"

        /// <summary>
        /// ID
        /// </summary>
        [XmlAttribute("Id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Is enabled
        /// </summary>
        [XmlAttribute("IsEnabled")]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Device type
        /// </summary>
        [XmlAttribute("DeviceType")]
        public DeviceType DeviceType
        {
            get { return deviceType; }
            set { deviceType = value; }
        }

        /// <summary>
        /// Guid of the device
        /// </summary>
        [XmlAttribute("DeviceInstanceGuid")]
        public Guid DeviceInstanceGuid
        {
            get { return deviceInstanceGuid; }
            set { deviceInstanceGuid = value; }
        }

        /// <summary>
        /// Input combination
        /// </summary>
        [XmlElement("InputCombination")]
        public List<int> InputCombination
        {
            get { return inputCombination; }
            set { inputCombination = value; }
        }

        [XmlElement("MappingActionAssemblyFileName")]
        public string MappingActionAssemblyFileName { get; set; }

        [XmlElement("MappingActionTypeName")]
        public string MappingActionTypeName { get; set; }

        [XmlElement("MappingActionConfiguration")]
        public SerializableDictionary<string, string> MappingActionConfiguration { get; set; }

        #endregion

        #region Creation

        internal static DeviceMapping CreateNewDeviceMapping()
        {
            return new DeviceMapping();
        }

        internal static DeviceMapping CreateDeviceMapping(
            bool isEnabled,
            Guid deviceInstanceGuid,
            DeviceType deviceType,
            List<int> inputCombination,
            Type mapActionType,
            IDictionary<string, string> mapActionConfiguration)
        {
            return new DeviceMapping(
                isEnabled,
                deviceInstanceGuid,
                deviceType,
                inputCombination,
                mapActionType,
                mapActionConfiguration);
        }

        #endregion // Creation

        #region "Constructors"
        public DeviceMapping()
        {
            //monitorWorker = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
            //monitorWorker.RunWorkerCompleted += BackgroundWorkerRunWorkerCompleted;
            //monitorWorker.ProgressChanged += BackgroundWorkerProgressChanged;
            //monitorWorker.DoWork += BackgroundWorkerDoWork;
            Id = Guid.NewGuid();
            IsEnabled = false;
            deviceType = DeviceType.Device;
            deviceInstanceGuid = Guid.Empty;
            inputCombination = new List<int>();
            MappingActionConfiguration = new SerializableDictionary<string, string>();
        }

        internal DeviceMapping(bool isEnabled, Guid deviceInstanceGuid, DeviceType deviceType, List<int> inputCombination,
            Type mapActionType, IDictionary<string, string> mapActionConfiguration)
        //: this()
        {
            IsEnabled = isEnabled;
            DeviceInstanceGuid = deviceInstanceGuid;
            DeviceType = deviceType;
            InputCombination = inputCombination;
            MappingActionAssemblyFileName = Path.GetFileName(mapActionType.Assembly.Location);
            MappingActionTypeName = mapActionType.FullName;
            MappingActionConfiguration = new SerializableDictionary<string, string>(mapActionConfiguration);
        }

        //~DeviceMapping()
        //{
        //    //Stop();
        //}
        #endregion

        //public void Start()
        //{
        //    Stop();
        //    while (monitorWorker.IsBusy)
        //    {
        //        Thread.Sleep(100);
        //    }
        //    if (!IsEnabled) return;
        //    var deviceInstance = new Device(deviceInstanceGuid);
        //    var windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
        //    deviceInstance.SetCooperativeLevel(windowHandle, CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
        //    notifier = new AutoResetEvent(false);
        //    deviceInstance.SetEventNotification(notifier);
        //    deviceInstance.Acquire();
        //    monitorWorker.RunWorkerAsync(deviceInstance);
        //}

        //public void Stop()
        //{
        //    if (monitorWorker.IsBusy) monitorWorker.CancelAsync();
        //}

        #region IEquatable<DeviceMapping> Members

        public bool Equals(DeviceMapping other)
        {
            var sameInputCombination = other.InputCombination.Count == InputCombination.Count;
            if (sameInputCombination)
            {
                foreach (var combination in other.InputCombination)
                {
                    if (InputCombination.Contains(combination)) continue;
                    sameInputCombination = false;
                    break;
                }
            }
            return sameInputCombination && deviceInstanceGuid.Equals(other.DeviceInstanceGuid);
        }

        #endregion

        //void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        //{
        //    var worker = (sender as BackgroundWorker);
        //    if (worker == null) return;
        //    e.Result = e.Argument;
        //    while (!worker.CancellationPending)
        //    {
        //        if (!notifier.WaitOne(100)) continue;
        //        notifier.Reset();
        //        worker.ReportProgress(0, e.Argument);
        //    }
        //}

        //void BackgroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //    var device = e.UserState as Device;
        //    var activated = false;
        //    if (device == null) return;
        //    switch (device.DeviceInformation.DeviceType)
        //    {
        //        case DeviceType.Keyboard:
        //            var pressedKeys = device.GetPressedKeys();
        //            if (pressedKeys != null && pressedKeys.Length > 0)
        //            {
        //                activated = pressedKeys.Length == InputCombination.Count;
        //                foreach (var key in pressedKeys)
        //                {
        //                    activated = activated && InputCombination.Contains((int)key);
        //                }
        //            }
        //            break;
        //        case DeviceType.Mouse:
        //            var buttons = device.CurrentMouseState.GetMouseButtons();
        //            if (buttons != null && buttons.Length > 0)
        //            {
        //                activated = buttons.Length == InputCombination.Count;
        //                for (var i = 0; i < buttons.Length; i++)
        //                {
        //                    if (buttons[i] != 0) activated = activated && InputCombination.Contains(i);
        //                }
        //            }
        //            break;
        //        case DeviceType.Flight:
        //        case DeviceType.Gamepad:
        //        case DeviceType.Joystick:
        //        case DeviceType.Remote:
        //        case DeviceType.ScreenPointer:
        //            buttons = device.CurrentJoystickState.GetButtons();
        //            if (buttons != null && buttons.Length > 0)
        //            {
        //                activated = buttons.Length == InputCombination.Count;
        //                for (var i = 0; i < buttons.Length; i++)
        //                {
        //                    if (buttons[i] != 0) activated = activated && InputCombination.Contains(i);
        //                }
        //            }
        //            break;
        //        default:
        //            // Not supported
        //            break;
        //    }

        //    if (activated)
        //    {
        //        // Do task
        //    }
        //}

        //static void BackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    var device = e.Result as Device;
        //    if (device != null)
        //    {
        //        device.Unacquire();
        //    }
        //}

        public override string ToString()
        {
            return DeviceInstanceGuid.Equals(Guid.Empty) ? "No mapping" : DisplayMapping();
        }

        private string DisplayMapping()
        {
            var presentation = string.Empty;
            if (deviceType == DeviceType.Keyboard)
            {
                foreach (var key in InputCombination)
                {
                    if (!string.IsNullOrEmpty(presentation)) presentation += " + ";
                    presentation += Enum.Parse(typeof(Key), key.ToString()).ToString();
                }
            }
            else
            {
                foreach (var buttonNumber in InputCombination)
                {
                    if (!string.IsNullOrEmpty(presentation)) presentation += " + ";
                    presentation += "B" + buttonNumber;
                }
            }
            presentation = string.Format("[{0}] {1}", deviceType, presentation);
            return presentation;
        }

        internal void SetMapActionType(Type mapActionType)
        {
            MappingActionAssemblyFileName = Path.GetFileName(mapActionType.Assembly.Location);
            MappingActionTypeName = mapActionType.FullName;
        }

        #region IDataErrorInfo Members

        string IDataErrorInfo.Error { get { return null; } }

        string IDataErrorInfo.this[string propertyName]
        {
            get { return GetValidationError(propertyName); }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Returns true if this object has no validation errors.
        /// </summary>
        internal bool IsValid
        {
            get
            {
                foreach (var property in ValidatedProperties)
                    if (GetValidationError(property) != null)
                        return false;

                return true;
            }
        }

        static readonly string[] ValidatedProperties = 
        { 
            "DeviceInstanceGuid", 
            "InputCombination", 
            "MappingActionAssemblyFileName",
            "MappingActionTypeName"
        };

        string GetValidationError(string propertyName)
        {
            if (Array.IndexOf(ValidatedProperties, propertyName) < 0)
                return null;

            string error = null;

            switch (propertyName)
            {
                case "DeviceInstanceGuid":
                    error = ValidateDeviceInstanceGuid();
                    break;

                case "InputCombination":
                    error = ValidateInputCombination();
                    break;

                case "MappingActionAssemblyFileName":
                    error = ValidateAssemblyFileName();
                    break;

                case "MappingActionTypeName":
                    error = ValidateTypeName();
                    break;

                default:
                    Debug.Fail("Unexpected property being validated on DeviceMapping: " + propertyName);
                    break;
            }

            return error;
        }

        string ValidateDeviceInstanceGuid()
        {
            return deviceInstanceGuid.Equals(Guid.Empty) ? Strings.DeviceMappingViewModel_Error_MissingDeviceInstanceGuid : null;
        }

        string ValidateInputCombination()
        {
            return inputCombination.Count == 0 ? Strings.DeviceMappingViewModel_Error_MissingInputCombination : null;
        }

        string ValidateAssemblyFileName()
        {
            return IsStringMissing(MappingActionAssemblyFileName) ? Strings.DeviceMappingViewModel_Error_MissingAssemblyFileName : null;
        }

        string ValidateTypeName()
        {
            return IsStringMissing(MappingActionTypeName) ? Strings.DeviceMappingViewModel_Error_MissingTypeName : null;
        }

        static bool IsStringMissing(string value)
        {
            return
                String.IsNullOrEmpty(value) ||
                value.Trim() == String.Empty;
        }
        #endregion // Validation
    }
}

