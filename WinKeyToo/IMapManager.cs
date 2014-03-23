using System;

namespace WinKeyToo
{
    internal interface IMapManager
    {
        IMapDevice Device(Guid deviceId);
        void Start();
        void Stop();
        void Clear();
    }
}