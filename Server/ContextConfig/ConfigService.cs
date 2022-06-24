using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
                            ModifiedAt = System.IO.File.GetLastWriteTime(f),
                            Status = Get(Path.GetFileNameWithoutExtension(f)).Enabled,
                            Mode = Get(Path.GetFileNameWithoutExtension(f)).Mode
                        }).OrderBy(x => x.Name);

            return envFiles;
        }

        public ConfigFile GetByName(string name)
        {
            var configFolder = GetConfigFolder();
            var files = Directory.GetFiles(configFolder, "*.json");

            var envFiles = files.Select(f =>
                        new ConfigFile
                        {
                            Name = Path.GetFileNameWithoutExtension(f),
                            Path = f,
                            ModifiedAt = System.IO.File.GetLastWriteTime(f),
                            Status = Get(Path.GetFileNameWithoutExtension(f)).Enabled,
                            Mode = Get(Path.GetFileNameWithoutExtension(f)).Mode
                        }).Where(x => x.Name == name);

            return envFiles.First();
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

        public SqlConnection CreateConnection(string connection)
        {
            SqlConnection conn = new SqlConnection(connection);
            return conn;
        }

        public void CreateDatapoint(DataPointConfig dataPoint)
        {
            var configFolder = GetConfigFolder();
            var fileName = dataPoint.ContextName + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configString = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<OtmContextConfig>(configString);
            var index = config.DataPoints != null ? config.DataPoints.Where(e => e.Id == dataPoint.Id).ToList() : null;

            if (index != null) {
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
            }
            else {
                config.DataPoints = new List<DataPointConfig>();
                dataPoint.Id = Guid.NewGuid();
                config.DataPoints.Add(dataPoint);
            }
            

            var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
            File.WriteAllText(configPath, configJson);
        }

        public void DeleteDataPoint(DataPointInput input) {
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

        //public SqlDataReader executeProcedure(DataPointConfig dataPoint){
        //    SqlConnection conn = new SqlConnection(dataPoint.Config);

        //    var cmd = conn.CreateCommand();
        //    cmd.CommandText = dataPoint.Name;
        //    cmd.CommandType = CommandType.StoredProcedure;

        //    foreach (var dp in dataPoint.Params)
        //    {
        //        if (dp.Direction == 0)
        //        {
        //            SqlParameter param = new SqlParameter();
        //            param.ParameterName = dp.Name;
        //            param.Size = (int)dp.Length;
        //            param.Value = dp.Value;
        //            cmd.Parameters.Add(param);
        //        }
        //        else {
        //            SqlParameter param = new SqlParameter();
        //            param.ParameterName = dp.Name;
        //            param.Size = (int)dp.Length;
        //            param.Direction = ParameterDirection.Output;
        //            cmd.Parameters.Add(param);
        //        }
        //    }

        //    conn.Open();

        //    SqlDataReader reader = cmd.ExecuteReader();
        //    return reader;
        //}
    }
}