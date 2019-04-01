using System.Collections.Generic;
using NLog;
using Otm.Config;
using Otm.DeviceDrivers;

namespace Otm.Device
{
    public interface IDeviceFactory
    {
        IDictionary<string, IDevice> CreateDevices(DeviceConfig[] config);
    }
}