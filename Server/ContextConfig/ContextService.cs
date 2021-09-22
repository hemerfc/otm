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
    public class ContextService : IContextService
    {
        public string GetConfigFolder()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var configPath = Path.Combine(appPath, "Configs");

            return configPath;
        }
        public void CreateOrEditContext(ContextInput Context)
        {

            var configFolder = GetConfigFolder();
            var fileName = Context.Name + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            if (File.Exists(configPath))
            {
                var configString = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<OtmContextConfig>(configString);
                config.Name = Context.Name;
                config.Enabled = Context.Enabled;
                var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
                File.WriteAllText(configPath, configJson);
            }
            else {
                var config = new OtmContextConfig();
                config.Name = Context.Name;
                config.Enabled = Context.Enabled;
                var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
                File.WriteAllText(configPath, configJson);
            }
        }

        public void DeleteContext(ContextInput input)
        {
            var configFolder = GetConfigFolder();
            var fileName = input.Name + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            File.Delete(configPath);        
        }
    }
}
