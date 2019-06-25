using System.Collections.Generic;
using NLog;
using Otm.Config;
using Otm.DeviceDrivers;
using Otm.Logger;

namespace Otm.Device
{
    public interface IDeviceFactory
    {
        IDictionary<string, IDevice> CreateDevices(DeviceConfig[] config, ILoggerFactory loggerFactory);
    }
}