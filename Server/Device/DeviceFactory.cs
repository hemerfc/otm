using System;
using System.Linq;
using System.Collections.Generic;
using Otm.Server.ContextConfig;
using Otm.Server.DeviceDrivers;
using Otm.Server.Device.S7;
using Otm.Shared.ContextConfig;
using Otm.Server.Device.Ptl;
using Otm.Server.Plugin;
using NLog;
using Otm.Server.Device.TcpServer;
using Otm.Server.Device.Palantir;

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

                    logger.Debug($"Device {dvConfig?.Name}: Intializing with driver '{dvConfig.Driver}'");

                    switch (dvConfig.Driver)
                    {
                        case "s7":
                            var client = new S7Client() { PduSizeRequested = 960 };
                            var s7Device = new S7Device();
                            s7Device.Init(dvConfig, client, logger);
                            devices.Add(dvConfig.Name, s7Device);
                            logger.Debug($"Device {dvConfig?.Name}: Created");
                            break;
                        case "ptl":
                            var ptlDevice = new AtopPtlDevice();
                            ptlDevice.Init(dvConfig, logger);
                            devices.Add(dvConfig.Name, ptlDevice);
                            //if (dvConfig.TipoPtl == "Atop")
                            //{
                            //    var ptlDevice = new AtopPtlDevice();
                            //    ptlDevice.Init(dvConfig, logger);
                            //    devices.Add(dvConfig.Name, ptlDevice);
                            //}
                            //else
                            //{
                            //    var ptlDevice = new SmartPickingPtlDevice();
                            //    ptlDevice.Init(dvConfig, logger);
                            //    devices.Add(dvConfig.Name, ptlDevice);
                            //}
                            //var ptlDevice = new PtlDevice();
                            //ptlDevice.Init(dvConfig, logger);
                            //devices.Add(dvConfig.Name, ptlDevice);
                            logger.Debug($"Device {dvConfig?.Name}: Created");
                            break;
                        case "RabbitMq":
                            var rabbitMqDevice = new RabbitMqDevice();
                            rabbitMqDevice.Init(dvConfig, logger);
                            devices.Add(dvConfig.Name, rabbitMqDevice);
                            logger.Debug($"Device {dvConfig?.Name}: Created");
                            break;
                        case "File":
                            var fileDevice = new FileDevice();
                            fileDevice.Init(dvConfig, logger);
                            devices.Add(dvConfig.Name, fileDevice);
                            logger.Debug($"Device {dvConfig?.Name}: Created");
                            break;
                        case "TCP_Server":
                            var TCPServerDevice = new TCPClientDevice();
                            TCPServerDevice.Init(dvConfig, logger);
                            devices.Add(dvConfig.Name, TCPServerDevice);
                            logger.Debug($"Device {dvConfig?.Name}: Created");
                            break;
                        case "pl":
                            var palantirDevice = new PalantirDevice();
                            palantirDevice.Init(dvConfig, logger);
                            devices.Add(dvConfig.Name, palantirDevice);
                            logger.Debug($"Device {dvConfig?.Name}: Created");
                            break;
                        default:
                            try
                            {
                                var pluginsNames = PluginLoadContext.GetDevicePlugins(logger);
                                var pluginName = pluginsNames.Single(x => x.Name == dvConfig.Driver);
                                var device = PluginLoadContext.LoadAndCreateDevicePlugin(pluginName.FileName, logger);
                                device.Init(dvConfig, logger);
                                devices.Add(dvConfig.Name, device);
                            }
                            catch (Exception ex)
                            {
                                var ex2 = new Exception("Invalid DeviceDriver in config. Driver:" + dvConfig.Driver, ex);
                                ex2.Data.Add("field", "Driver");
                                throw ex2;
                            }
                            break;
                    }
                }

            return devices;
        }
    }
}