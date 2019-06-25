using System;
using System.Collections.Generic;
using NLog;
using Otm.Config;
using Otm.DeviceDrivers;
using Otm.Logger;

namespace Otm.Device
{
    public class DeviceFactory : IDeviceFactory
    {
        public IDictionary<string, IDevice> CreateDevices(DeviceConfig[] devicesConfig, ILoggerFactory loggerFactory)
        {
            var devices = new Dictionary<string, IDevice>();

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
                        devices.Add(dvConfig.Name, new S7Device(dvConfig, new Snap7.S7ClientFactory(), loggerFactory));
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