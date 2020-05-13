using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentValidation.Results;

namespace Otm.ContextConfig
{
    public class ConfigService : IConfigService
    {
        public IEnumerable<ConfigFile> GetFiles()
        {
            var configFolder = GetConfigFolder();
            var files = Directory.GetFiles(configFolder, "*.json");

            var envFiles = files.Select(f =>
                        new ConfigFile
                        {
                            Name = Path.GetFileNameWithoutExtension(f),
                            Path = f,
                            ModifiedAt = System.IO.File.GetLastWriteTime(f)
                        }).OrderBy(x => x.Name);

            return envFiles;
        }

        public string GetConfigFolder()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var configPath = Path.Combine(appPath, "Configs");

            return configPath;
        }

        public RootConfig LoadConfig(string configName)
        {
            var configFolder = GetConfigFolder();
            var fileName = configName + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configString = File.ReadAllText(configPath);
            var rootconfig = JsonSerializer.Deserialize<RootConfig>(configString);
            return rootconfig;
        }

        public ValidationResult ValidateCreateConfig(RootConfig config)
        {
            var result = new ValidationResult();

            var validName = config.Name != null && config.Name.All(c => Char.IsLetterOrDigit(c) || c == '_');

            if (!validName)
            {
                result.Errors.Add(new ValidationFailure(nameof(config.Name), "Nome do ambiente invalido! Use apenas letras, numeros e '_'."));
            }

            var configFolder = GetConfigFolder();
            var fileName = config.Name + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            if (File.Exists(configPath))
            {
                result.Errors.Add(new ValidationFailure(nameof(config.Name), "Nome do ambiente invalido! Ja existe um ambiente com este nome."));
            }

            return result;
        }

        public ValidationResult ValidateEditConfig(string oldConfigId, RootConfig config)
        {
            var result = new ValidationResult();

            var validName = config.Name != null && config.Name.All(c => Char.IsLetterOrDigit(c) || c == '_');

            if (!validName)
            {
                result.Errors.Add(new ValidationFailure(nameof(config.Name), "Nome do ambiente invalido! Use apenas letras, numeros e '_'."));
            }

            var configFolder = GetConfigFolder();
            var fileName = oldConfigId + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            if (!File.Exists(configPath))
            {
                result.Errors.Add(new ValidationFailure(nameof(config.Name), "O arquivo orginal do ambiente editado nao existe."));
            }

            var newFileName = config.Name + ".json";
            var newConfigPath = Path.Combine(configFolder, newFileName);
            if (File.Exists(newConfigPath))
            {
                result.Errors.Add(new ValidationFailure(nameof(config.Name), "Nome do ambiente invalido! Ja existe um ambiente com este nome."));
            }

            return result;
        }

        public void CreateConfig(RootConfig config)
        {
            var configFolder = GetConfigFolder();
            var fileName = config.Name + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configJson = JsonSerializer.Serialize<RootConfig>(config);
            File.WriteAllText(configPath, configJson);
        }

        public void EditConfig(string oldConfigId, RootConfig config)
        {
            var configFolder = GetConfigFolder();
            var fileName = config.Name + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configJson = JsonSerializer.Serialize<RootConfig>(config);
            File.WriteAllText(configPath, configJson);

            if (config.Name != oldConfigId)
            {
                DeleteConfig(oldConfigId);
            }
        }

        public void DeleteConfig(string configId)
        {
            var configFolder = GetConfigFolder();
            var fileName = configId + ".json";
            var configPath = Path.Combine(configFolder, fileName);
            File.Delete(configPath);
        }
    }
}