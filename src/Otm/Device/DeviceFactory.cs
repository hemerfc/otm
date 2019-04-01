using System.Collections.Generic;
using NLog;
using Otm.Config;
using Otm.DeviceDrivers;

namespace Otm.Device
{
    public class DeviceFactory : IDeviceFactory
    {
        public IDictionary<string, IDevice> CreateDevices(DeviceConfig[] config)
        {
            throw new System.NotImplementedException();
        }
    }
}