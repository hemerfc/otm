using System.Collections.Generic;
using NLog;
using Otm.ContextConfig;
using Otm.DeviceDrivers;
using Otm.Logger;

namespace Otm.Device
{
    public interface IDeviceFactory
    {
        IDictionary<string, IDevice> CreateDevices(List<DeviceConfig> config);
    }
}