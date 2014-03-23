using System;
using Microsoft.DirectX.DirectInput;

namespace WinKeyToo
{
    internal class MappableDeviceInformation
    {
        public string Name { get; set; }
        public DeviceInstance Instance { get; set; }

        public MappableDeviceInformation(DeviceInstance deviceInstance)
        {
            Name = deviceInstance.InstanceName;
            Instance = deviceInstance;
        }

        public MappableDeviceInformation(string name)
        {
            Instance = new DeviceInstance();
            Name = name;
        }

        public bool IsDevice
        {
            get { return !Instance.InstanceGuid.Equals(Guid.Empty); }
        }
        public override int GetHashCode()
        {
            return Instance.InstanceGuid.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is MappableDeviceInformation)
            {
                var deviceInformation = obj as MappableDeviceInformation;
                return deviceInformation.Instance.InstanceGuid.Equals(Instance.InstanceGuid);
            }
            return base.Equals(obj);
        }
    }
}