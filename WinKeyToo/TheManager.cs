using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DirectX.DirectInput;

namespace WinKeyToo
{
    internal class TheManager : IMapManager
    {
        private Dictionary<Guid, IMapDevice> MappedDevices { get; set; }

        private TheManager()
        {
            MappedDevices = new Dictionary<Guid, IMapDevice>();
        }

        private static bool LocateDeviceByGuid(Guid deviceId, out DeviceInstance returnedInstance)
        {
            var located = false;
            //Populate All devices
            foreach (DeviceInstance di in Manager.Devices)
            {
                if (!di.InstanceGuid.Equals(deviceId)) continue;
                located = true;
                returnedInstance = di;
                break;
            }
            return located;
        }

        //public event DeviceInputEventHandler DeviceInputEvent;
        //protected virtual void OnDeviceInputEvent()
        //{
        //    var handler = DeviceInputEvent;
        //    if (handler != null) handler(this, new DeviceInputEventArgs());
        //}

        //public void Listen(DeviceInputListeningOptions options, DeviceInputEventHandler handler)
        //{
        //    DeviceInputEvent += handler;
        //}

        private static TheManager instance;
        private static IMapManager GetInstance()
        {
            if (instance == null) instance = new TheManager();
            return instance;
        }
        public static IMapManager Using
        {
            get { return GetInstance(); }
        }
        public static IMapManager Current
        {
            get { return GetInstance(); }
        }

        public IMapDevice Device(Guid deviceId)
        {
            DeviceInstance deviceInstance;
            var mappedDevice = (from md in MappedDevices where md.Key == deviceId select md.Value).SingleOrDefault();
            if (mappedDevice != null) return mappedDevice;
            if (LocateDeviceByGuid(deviceId, out deviceInstance))
            {
                mappedDevice = new MapDevice(deviceInstance);
                MappedDevices.Add(deviceId, mappedDevice);
            }
            else return null; //  throw new ArgumentException("Device not found: " + deviceId);
            return mappedDevice;
        }

        public void Start()
        {
            foreach (var mappedDevice in MappedDevices) mappedDevice.Value.Start();
        }

        public void Stop()
        {
            foreach (var mappedDevice in MappedDevices) mappedDevice.Value.Stop();
        }

        #region IMapManager Members


        public void Clear()
        {
            Stop();
            MappedDevices.Clear();
            GC.Collect();
        }

        #endregion
    }
}
