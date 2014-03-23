using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Microsoft.DirectX.DirectInput;
using WinKeyToo.Instrumentation;

namespace WinKeyToo
{
    internal class MapDevice : INotifyPropertyChanged, IMapDevice
    {
        private readonly List<IMapSequence> mapSequences;
        private AutoResetEvent notifier;
        private readonly BackgroundWorker monitorWorker;

        private DeviceInstance directInputDevice;
        public DeviceInstance DirectInputDevice
        {
            get
            {
                return directInputDevice;
            }
            set
            {
                directInputDevice = value;
                OnPropertyChanged("DirectInputDevice");
                OnPropertyChanged("DeviceName");
            }
        }

        public string Name
        {
            get
            {
                return DirectInputDevice.InstanceName;
            }
        }

        public MapDevice()
        {
            mapSequences = new List<IMapSequence>();
            monitorWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            monitorWorker.RunWorkerCompleted += BackgroundWorkerRunWorkerCompleted;
            monitorWorker.ProgressChanged += BackgroundWorkerProgressChanged;
            monitorWorker.DoWork += BackgroundWorkerDoWork;
        }

        public MapDevice(DeviceInstance directInputDevice) : this()
        {
            DirectInputDevice = directInputDevice;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region IMappableDevice Members

        public IMapSequence Map(int input)
        {
            var newSequence = new MapSequence(input);
            mapSequences.Add(newSequence);
            return newSequence;
        }

        public IMapSequence StartNewSequence
        {
            get
            {
                var newSequence = new MapSequence();
                mapSequences.Add(newSequence);
                return newSequence;
            }
        }

        public void Start()
        {
            using (var tracing = new Tracing(false))
            {
                try
                {
                    Stop();
                    while (monitorWorker.IsBusy)
                    {
                        Thread.Sleep(50);
                    }
                    //if (!IsEnabled) return;
                    var deviceInstance = new Device(DirectInputDevice.InstanceGuid);
                    var windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                    deviceInstance.SetCooperativeLevel(windowHandle,
                                                       CooperativeLevelFlags.Background |
                                                       CooperativeLevelFlags.NonExclusive);
                    notifier = new AutoResetEvent(false);
                    deviceInstance.SetEventNotification(notifier);
                    deviceInstance.Acquire();
                    tracing.WriteInfo(deviceInstance.DeviceInformation.InstanceName + " acquired");
                    monitorWorker.RunWorkerAsync(deviceInstance);
                }
                catch (Microsoft.DirectX.DirectXException dxEx)
                {
                    tracing.WriteError(dxEx);
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                    Stop();
                }
            }
        }

        public void Stop()
        {
            using (var tracing = new Tracing(false))
            {
                try
                {
                    if (monitorWorker.IsBusy) monitorWorker.CancelAsync();
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }

        #endregion

        void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            using (var tracing = new Tracing(false))
            {
                try
                {
                    var worker = (sender as BackgroundWorker);
                    if (worker == null) return;
                    e.Result = e.Argument;
                    while (!worker.CancellationPending && worker.IsBusy)
                    {
                        if (!notifier.WaitOne(50)) continue;
                        notifier.Reset();
                        worker.ReportProgress(0, e.Argument);
                    }
                }
                catch (Microsoft.DirectX.DirectXException dxEx)
                {
                    tracing.WriteError(dxEx);
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }

        void BackgroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            using (var tracing = new Tracing(false))
            {
                try
                {
                    var device = e.UserState as Device;
                    //var activated = false;
                    if (device == null) return;
                    switch (device.DeviceInformation.DeviceType)
                    {
                        case DeviceType.Keyboard:
                            var pressedKeys = device.GetPressedKeys();
                            if (pressedKeys != null && pressedKeys.Length > 0)
                            {
                                //activated = pressedKeys.Length == InputCombination.Count;
                                foreach (var mapSequence in mapSequences)
                                {
                                    mapSequence.Receive(pressedKeys);
                                    //foreach (var key in pressedKeys)
                                    //{
                                    //    mapSequence.Receive((int) key);
                                    //    //activated = activated && InputCombination.Contains((int)key);
                                    //}
                                }
                            }
                            break;
                        case DeviceType.Mouse:
                            var buttons = device.CurrentMouseState.GetMouseButtons();
                            if (buttons != null && buttons.Length > 0)
                            {
                                var inputs = new List<int>();
                                for (var i = 0; i < buttons.Length; i++)
                                {
                                    if (buttons[i] != 0) inputs.Add(i);
                                }
                                if (inputs.Count > 0)
                                {
                                    //activated = buttons.Length == InputCombination.Count;
                                    foreach (var mapSequence in mapSequences)
                                    {
                                        mapSequence.Receive(buttons);
                                    }
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
                                var inputs = new List<int>();
                                for (var i = 0; i < buttons.Length; i++)
                                {
                                    if (buttons[i] != 0) inputs.Add(i);
                                }
                                if (inputs.Count > 0)
                                {
                                    //activated = buttons.Length == InputCombination.Count;
                                    foreach (var mapSequence in mapSequences)
                                    {
                                        mapSequence.Receive(inputs.ToArray());
                                    }
                                }
                            }
                            break;
                        default:
                            // Not supported
                            break;
                    }

                    //if (activated)
                    //{
                    //    // Do task
                    //}
                }
                catch (Microsoft.DirectX.DirectXException dxEx)
                {
                    tracing.WriteError(dxEx);
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }

        static void BackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            using (var tracing = new Tracing(false))
            {
                try
                {
                    var device = e.Result as Device;
                    if (device != null)
                    {
                        device.Unacquire();
                        tracing.WriteInfo(device.DeviceInformation.InstanceName + " unacquired");
                        device.Dispose();
                    }
                }
                catch (Microsoft.DirectX.DirectXException dxEx)
                {
                    tracing.WriteError(dxEx);
                }
                catch (Exception ex)
                {
                    tracing.WriteError(ex);
                }
            }
        }
    }
}
