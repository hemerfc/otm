using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using Otm.Server.Device;

namespace Otm.Server.Plugin
{
    class PluginLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
        public static Assembly LoadPlugin(string relativePath)
        {
            // Navigate up to the solution root
            string root = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(
                                Path.GetDirectoryName(typeof(Program).Assembly.Location)))))));

            string pluginLocation = Path.GetFullPath(Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            Console.WriteLine($"Loading commands from: {pluginLocation}");
            PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }

        public static IDevice LoadAndCreateDevicePlugin(string pluginPath)
        {
            PluginLoadContext loadContext = new PluginLoadContext(pluginPath);
            var assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginPath)));

            int count = assembly.GetTypes().Where(a => typeof(IDevice).IsAssignableFrom(a)).Count();

            if (count > 1)
            {
                string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
                throw new ApplicationException(
                    $"Duplicated type which implements IDevice in {assembly} from {assembly.Location}.\n" +
                    $"Available types: {availableTypes}");
            }

            if (count == 0)
            {
                string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
                throw new ApplicationException(
                    $"Can't find any type which implements IDevice in {assembly} from {assembly.Location}.\n" +
                    $"Available types: {availableTypes}");
            }


            Type type = assembly.GetTypes().Single(a => typeof(IDevice).IsAssignableFrom(a));

            IDevice result = Activator.CreateInstance(type) as IDevice;

            return result;
        }

        public static IEnumerable<(string FileName, string Name)> GetDevicePlugins()
        {
            //var deviceColector = colectorService.CreateDeviceColector(dvConfig.Name);
            var pluginsPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            pluginsPath = Path.Combine(pluginsPath, "Plugins");
            pluginsPath = Path.Combine(pluginsPath, "Devices");
            var pluginsFiles = Directory.GetFiles(pluginsPath, "Otm.Plugins.Devices.*.dll", SearchOption.AllDirectories);
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
