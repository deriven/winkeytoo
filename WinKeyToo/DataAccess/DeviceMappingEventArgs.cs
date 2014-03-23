using System;
using WinKeyToo.Model;

namespace WinKeyToo.DataAccess
{
    internal class DeviceMappingEventArgs : EventArgs
    {
        public DeviceMappingEventArgs(DeviceMapping deviceMapping)
        {
            DeviceMapping = deviceMapping;
        }

        public DeviceMapping DeviceMapping { get; private set; }
    }
}