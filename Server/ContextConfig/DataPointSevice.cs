using Otm.Shared.ContextConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Otm.Server.ContextConfig
{
    public class DataPointSevice: IDataPointService
    {
        public void CreateDatapoint(DataPointConfig dataPoint)
        {
            var configFolder = GetConfigFolder();
            var fileName = dataPoint.ContextName + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configString = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<OtmContextConfig>(configString);
            var index = config.DataPoints.Where(e => e.Id == dataPoint.Id).ToList();

            if (index.Count() > 0)
            {
                foreach (var dp in config.DataPoints)
                {
                    if (dp.Id == dataPoint.Id)
                    {
                        dp.Name = dataPoint.Name;
                        dp.Config = dataPoint.Config;
                        dp.DebugMessages = dataPoint.DebugMessages;
                        dp.Params = dataPoint.Params;
                    }
                }
            }
            else
            {
                dataPoint.Id = Guid.NewGuid();
                config.DataPoints.Add(dataPoint);
            }

            var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
            File.WriteAllText(configPath, configJson);
        }

        public void DeleteDataPoint(DataPointInput input)
        {
            var configFolder = GetConfigFolder();
            var fileName = input.ContextName + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configString = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<OtmContextConfig>(configString);

            var index = config.DataPoints.FindIndex(row => row.Id == input.Id);
            config.DataPoints.RemoveAt(index);

            var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
            File.WriteAllText(configPath, configJson);
        }

        public string GetConfigFolder()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var configPath = Path.Combine(appPath, "Configs");

            return configPath;
        }
    }
}
