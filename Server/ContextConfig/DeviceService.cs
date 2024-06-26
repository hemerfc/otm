﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentValidation.Results;
using Otm.Server.ContextConfig;

namespace Otm.Server.ContextConfig
{
    public class DeviceService : IDeviceService
    {

        public string GetConfigFolder()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var configPath = Path.Combine(appPath, "Configs");

            return configPath;
        }

        public void CreateOrEditDevice(DeviceConfig device)
        {
            var configFolder = GetConfigFolder();
            var fileName = device.ContextName + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configString = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<OtmContextConfig>(configString);
            var index = config.Devices != null ? config.Devices.Where(e => e.Id == device.Id).ToList() : null;

            if (index != null)
            {
                if (index.Count() > 0)
                {
                    foreach (var de in config.Devices)
                    {
                        if (de.Id == device.Id)
                        {
                            de.Name = device.Name;
                            de.Config = device.Config;
                            de.Driver = device.Driver;
                            de.TipoPtl = device.TipoPtl;
                            de.Tags = device.Tags;
                        }
                    }
                }
                else
                {
                    device.Id = Guid.NewGuid();
                    config.Devices.Add(device);
                }
            }
            else {
                config.Devices = new List<DeviceConfig>();
                device.Id = Guid.NewGuid();
                config.Devices.Add(device);
            }


            var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
            File.WriteAllText(configPath, configJson);
        }

        public void DeleteDevice(DeviceInput input)
        {
            var configFolder = GetConfigFolder();
            var fileName = input.ContextName + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configString = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<OtmContextConfig>(configString);

            var index = config.Devices.FindIndex(row => row.Id == input.Id);
            config.Devices.RemoveAt(index);

            var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
            File.WriteAllText(configPath, configJson);
        }

    }
}
