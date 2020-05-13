using System;
using System.Collections.Generic;
using NLog;
using Otm.ContextConfig;
using Otm.DeviceDrivers;
using Otm.Logger;
using Otm.Device.S7;

namespace Otm.Device
{
    public class DeviceFactory : IDeviceFactory
    {
        private static readonly ILogger Logger = LoggerFactory.GetCurrentClassLogger();

        public IDictionary<string, IDevice> CreateDevices(List<DeviceConfig> devicesConfig)
        {
            var devices = new Dictionary<string, IDevice>();
            var logger = LoggerFactory.GetCurrentClassLogger();

            if (devicesConfig != null)
                foreach (var dvConfig in devicesConfig)
                {
                    if (string.IsNullOrWhiteSpace(dvConfig.Name))
                    {
                        var ex = new Exception("Invalid Device name in config. Name:" + dvConfig.Name);
                        ex.Data.Add("field", "Name");
                        throw ex;
                    }

                    switch (dvConfig.Driver)
                    {
                        case "s7":
                            devices.Add(dvConfig.Name, new S7Device(dvConfig, new S7ClientFactory()));
                            logger.Error($"Device {dvConfig?.Name}: Created");
                            break;
                        default:
                            var ex = new Exception("Invalid DeviceDriver in config. Driver:" + dvConfig.Driver);
                            ex.Data.Add("field", "Driver");
                            throw ex;
                    }
                }

            return devices;
        }
    }
}