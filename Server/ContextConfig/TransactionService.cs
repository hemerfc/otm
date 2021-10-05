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
    public class TransactionService: ITransactionService
    {
        public string GetConfigFolder()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var configPath = Path.Combine(appPath, "Configs");

            return configPath;
        }

        public void CreateOrEditTransaction(TransactionConfig transaction)
        {
            var configFolder = GetConfigFolder();
            var fileName = transaction.ContextName + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configString = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<OtmContextConfig>(configString);
            var index = config.Transactions != null ? config.Transactions.Where(e => e.Id == transaction.Id).ToList() : null;

            if (index != null)
            {
                if (index.Count() > 0)
                {
                    foreach (var de in config.Transactions)
                    {
                        if (de.Id == transaction.Id)
                        {
                            de.Name = transaction.Name;
                            de.DataPointName = transaction.DataPointName;
                            de.SourceDeviceName = transaction.SourceDeviceName;
                            de.TargetDeviceName = transaction.TargetDeviceName;
                            de.TriggerTagName = transaction.TriggerTagName;
                            de.TriggerTime = transaction.TriggerTime;
                            de.TriggerType = transaction.TriggerType;
                            de.SourceBinds = transaction.SourceBinds;
                            de.TargetBinds = transaction.TargetBinds;
                        }
                    }
                }
                else
                {
                    transaction.Id = Guid.NewGuid();
                    config.Transactions.Add(transaction);
                }
            }
            else {
                config.Transactions = new List<TransactionConfig>();
                transaction.Id = Guid.NewGuid();
                config.Transactions.Add(transaction);
            }
            

            var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
            File.WriteAllText(configPath, configJson);
        }

        public void DeleteTransaction(TransactionInput input)
        {
            var configFolder = GetConfigFolder();
            var fileName = input.ContextName + ".json";
            var configPath = Path.Combine(configFolder, fileName);

            var configString = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<OtmContextConfig>(configString);

            var index = config.Transactions.FindIndex(row => row.Id == input.Id);
            config.Transactions.RemoveAt(index);

            var configJson = JsonSerializer.Serialize<OtmContextConfig>(config);
            File.WriteAllText(configPath, configJson);
        }
    }
}
