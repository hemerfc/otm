using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentValidation.Results;
using Otm.Shared.ContextConfig;

namespace Otm.Server.ContextConfig
{
    public class ConfigService : IConfigService
    {
        public IEnumerable<ConfigFile> GetAll()
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

        public OtmContextConfig Get(string id)
        {
            var configFolder = GetConfigFolder();
            var fileName = id + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configString = File.ReadAllText(configPath);
            var rootconfig = JsonSerializer.Deserialize<OtmContextConfig>(configString);
            return rootconfig;
        }

        public ValidationResult ValidateCreate(OtmContextConfig config)
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

        public ValidationResult ValidateUpdate(string oldId, OtmContextConfig config)
        {
            var result = new ValidationResult();

            var validName = config.Name != null && config.Name.All(c => Char.IsLetterOrDigit(c) || c == '_');

            if (!validName)
            {
                result.Errors.Add(new ValidationFailure(nameof(config.Name), "Nome do ambiente invalido! Use apenas letras, numeros e '_'."));
            }

            var configFolder = GetConfigFolder();
            var fileName = oldId + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            if (!File.Exists(configPath))
            {
                result.Errors.Add(new ValidationFailure(nameof(config.Name), "O arquivo orginal do ambiente editado nao existe."));
            }

            if (oldId != config.Name)
            {
                var newFileName = config.Name + ".json";
                var newConfigPath = Path.Combine(configFolder, newFileName);
                if (File.Exists(newConfigPath))
                {
                    result.Errors.Add(new ValidationFailure(nameof(config.Name), "Nome do ambiente invalido! Ja existe um ambiente com este nome."));
                }
            }

            return result;
        }

        public void Create(OtmContextConfig config)
        {
            var configFolder = GetConfigFolder();
            var fileName = config.Name + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
            File.WriteAllText(configPath, configJson);
        }

        public void Update(string oldId, OtmContextConfig config)
        {
            var configFolder = GetConfigFolder();
            var fileName = config.Name + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
            File.WriteAllText(configPath, configJson);

            if (config.Name != oldId)
            {
                Delete(oldId);
            }
        }

        public bool Exists(string id)
        {
            var configFolder = GetConfigFolder();
            var fileName = id + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            return File.Exists(configPath);
        }

        public void Delete(string id)
        {
            var configFolder = GetConfigFolder();
            var fileName = id + ".json";
            var configPath = Path.Combine(configFolder, fileName);
            File.Delete(configPath);
        }
    }
}