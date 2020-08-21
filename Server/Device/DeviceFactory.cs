using System;
using System.Collections.Generic;
using Otm.Server.ContextConfig;
using Otm.Server.DeviceDrivers;
using Otm.Server.Device.S7;
using Microsoft.Extensions.Logging;
using Otm.Shared.ContextConfig;
using Otm.Server.Device.Ptl;

namespace Otm.Server.Device
{
    public static class DeviceFactory
    {
        public static IDictionary<string, IDevice> CreateDevices(List<DeviceConfig> devicesConfig, ILogger logger)
        {
            var devices = new Dictionary<string, IDevice>();

            if (devicesConfig != null)
                foreach (var dvConfig in devicesConfig)
                {
                    if (string.IsNullOrWhiteSpace(dvConfig.Name))
                    {
                        var ex = new Exception("Invalid Device name in config. Name:" + dvConfig.Name);
                        ex.Data.Add("field", "Name");
                        throw ex;
                    }

                    //var deviceColector = colectorService.CreateDeviceColector(dvConfig.Name);

                    switch (dvConfig.Driver)
                    {
                        case "s7":
                            devices.Add(dvConfig.Name, new S7Device(dvConfig, new S7Client(), logger));
                            logger.LogError($"Device {dvConfig?.Name}: Created");
                            break;
                        case "ptl":
                            devices.Add(dvConfig.Name, new PtlDevice(dvConfig, new TcpClientAdapter(), logger));
                            logger.LogError($"Device {dvConfig?.Name}: Created");
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