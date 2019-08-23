using System;
using System.Collections.Generic;
using NLog;
using Otm.Config;
using Otm.DataPoint;
using Otm.Device;
using Otm.Logger;

namespace Otm.Transaction
{
    public class TransactionFactory : ITransactionFactory
    {
        public IDictionary<string, ITransaction> CreateTransactions(
            IEnumerable<TransactionConfig> transactionsConfig, 
            IDictionary<string, IDataPoint> dataPoints, 
            IDictionary<string, IDevice> devices, 
            ILoggerFactory loggerFactory)
        {
            var transactions = new Dictionary<string, ITransaction>();

            foreach (var trConfig in transactionsConfig)
            {
                // verify transaction name
                if (string.IsNullOrWhiteSpace(trConfig.Name))
                {
                    var ex = new Exception($"Invalid Transaction name in config. Name: {trConfig.Name}");
                    ex.Data.Add("field", "Name");
                    throw ex;
                }

                // verify the datapoint name
                if (!dataPoints.ContainsKey(trConfig.DataPointName) && dataPoints[trConfig.DataPointName] == null)
                {
                    var ex = new Exception($"Invalid DataPointName name in Transaction config. DataPointName ({trConfig.DataPointName}) Transaction ({trConfig.Name})");
                    ex.Data.Add("field", "DataPointName");
                    throw ex;
                }             

                // verify the trigger type
                if (trConfig.TriggerType != "on_tag_change")
                {
                    var ex = new Exception($"Invalid TriggerType Transaction config. TriggerType ({trConfig.TriggerType }) Transaction ({trConfig.Name})");
                    ex.Data.Add("field", "TriggerType");
                    throw ex;
                }   
                
                // verify the device name
                if (!devices.ContainsKey(trConfig.DeviceName) && devices[trConfig.DeviceName] == null)
                {
                    var ex = new Exception($"Invalid DeviceName name in Transaction config. DeviceName ({trConfig.DeviceName}) Transaction ({trConfig.Name})");
                    ex.Data.Add("field", "DeviceName");
                    throw ex;
                }                

                // verify the trigger tag name
                if (!devices[trConfig.DeviceName].ContainsTag(trConfig.TriggerTagName))
                {
                    var ex = new Exception($"Invalid TriggerTagName name in Transaction config. TriggerTagName ({trConfig.TriggerTagName}) Device ({trConfig.DeviceName}) Transaction ({trConfig.Name})");
                    ex.Data.Add("field", "Name");
                    throw ex;
                }

                foreach (var bind in trConfig.Binds)
                {
                    // verify each bind
                    var dpParam = dataPoints[trConfig.DataPointName].GetParamConfig(bind.DataPointParam);
                    var dvTag = devices[trConfig.DeviceName].GetTagConfig(bind.DeviceTag);

                    // verify the DataPointParam of bind
                    if (dpParam == null)
                    {
                        var ex = new Exception($"Invalid DataPointParam name in Transaction config. DataPointParam ({bind.DataPointParam}) Transaction ({trConfig.Name})");
                        ex.Data.Add("field", "Name");
                        throw ex;
                    }

                    // verify the DeviceTag of bind
                    if (dvTag == null)
                    {
                        var ex = new Exception($"Invalid DeviceTag name in Transaction config. DeviceTag ({bind.DeviceTag}) Transaction ({trConfig.Name})");
                        ex.Data.Add("field", "Name");
                        throw ex;
                    }

                    var compatModes = (dvTag.Mode == "in" && dpParam.Mode == "out") || 
                                    (dvTag.Mode == "out" && dpParam.Mode == "in");
                    if (!compatModes)
                    {
                        var ex = new Exception($"Invalid modes between DataPointParam and DeviceTag in Transaction config (valid is in => out or out => in). DataPointParam ({bind.DataPointParam}) DeviceTag ({bind.DeviceTag}) Transaction ({trConfig.Name})");
                        ex.Data.Add("field", "Name");
                        throw ex;
                    }
                    
                    if (dvTag.Type != dpParam.Type)
                    {
                        var ex = new Exception($"Invalid data type between DataPointParam and DeviceTag in Transaction config. DataPointParam ({bind.DataPointParam}) DeviceTag ({bind.DeviceTag}) Transaction ({trConfig.Name})");
                        ex.Data.Add("field", "Name");
                        throw ex;
                    }
                }

                var transaction = new Transaction(trConfig, devices[trConfig.DeviceName], dataPoints[trConfig.DataPointName]);
                transactions[trConfig.Name] = transaction;
            }

            return transactions;
        }
    }
}