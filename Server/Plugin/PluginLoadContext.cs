using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using Otm.Server.Device;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Logging;

namespace Otm.Server.Plugin
{
    class PluginLoadContext
    {

        public static IDevice LoadAndCreateDevicePlugin(string pluginPath, ILogger logger)
        {
            var loader = PluginLoader.CreateFromAssemblyFile(pluginPath, sharedTypes: new[] { typeof(IDevice) });

            foreach (var pluginType in loader
                    .LoadDefaultAssembly()
                    .GetTypes()
                    .Where(t => typeof(IDevice).IsAssignableFrom(t) && !t.IsAbstract))
            {
                // This assumes the implementation of IPlugin has a parameterless constructor
                IDevice plugin = (IDevice)Activator.CreateInstance(pluginType);

                //Console.WriteLine($"Created plugin instance '{plugin}'.");

                return plugin;
            }

            return null;
        }

        public static IEnumerable<(string FileName, string Name)> GetDevicePlugins(ILogger logger)
        {
            //var deviceColector = colectorService.CreateDeviceColector(dvConfig.Name);
            var pluginsPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            pluginsPath = Path.Combine(pluginsPath, "Plugins");
            pluginsPath = Path.Combine(pluginsPath, "Devices");
            var pluginsFiles = Directory.GetFiles(pluginsPath, "Otm.Plugins.Devices.*.dll", SearchOption.AllDirectories);

            /*
            logger.LogInformation($"Plugins in {pluginsPath}");
            foreach (var item in pluginsFiles)
            {
                logger.LogInformation(item);
            }
            */

            pluginsFiles = pluginsFiles.Where(x => !x.Contains("\\ref\\")).ToArray();
            var pluginsNames = pluginsFiles.Select(x =>
                (FileName: x, Name: Path.GetFileNameWithoutExtension(x).Split('.').Last())
            );
            var duplicates = pluginsNames.GroupBy(x => x.Name).Where(g => g.Count() > 1);

            // se contem plugins com nomes duplicados
            if (duplicates.Any())
            {
                foreach (var dup in duplicates)
                {
                    var msg = $"Device plugin duplicado encontrado {dup.Key}: Corrija para iniciar o OTM.";
                    throw new Exception(msg);
                }
            }

            return pluginsNames;
        }
    }
}
