using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Otm.Server.ContextConfig;
using Otm.Server.DataPoint;
using Otm.Server.Device;
using Otm.Shared.ContextConfig;

namespace Otm.Server.Transaction
{
    public static class TransactionFactory
    {
        public static IDictionary<string, ITransaction> CreateTransactions(
            IEnumerable<TransactionConfig> transactionsConfig,
            IDictionary<string, IDataPoint> dataPoints,
            IDictionary<string, IDevice> devices,
            ILogger logger)
        {
            var transactions = new Dictionary<string, ITransaction>();

            if (transactionsConfig != null)
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

                    // verify the trigger OnTagChange
                    if (trConfig.TriggerType == TriggerTypes.OnTagChange && string.IsNullOrWhiteSpace(trConfig.TriggerTagName))
                    {
                        var ex = new Exception($"Invalid TriggerTagName, can't be empty. TriggerType ({trConfig.TriggerType }) Transaction ({trConfig.Name})");
                        ex.Data.Add("field", "TriggerTagName");
                        throw ex;
                    }

                    // verify the trigger tag name
                    if (trConfig.TriggerType == TriggerTypes.OnTagChange && !devices[trConfig.DeviceName].ContainsTag(trConfig.TriggerTagName))
                    {
                        var ex = new Exception($"Invalid TriggerTagName name in Transaction config. TriggerTagName ({trConfig.TriggerTagName}) Device ({trConfig.DeviceName}) Transaction ({trConfig.Name})");
                        ex.Data.Add("field", "Name");
                        throw ex;
                    }

                    // verify the trigger OnCycle
                    if (trConfig.TriggerType == TriggerTypes.OnCycle && trConfig.TriggerTime == 0)
                    {
                        var ex = new Exception($"Invalid TriggerTime, can't be zero. TriggerType ({trConfig.TriggerType }) Transaction ({trConfig.Name})");
                        ex.Data.Add("field", "TriggerTime");
                        throw ex;
                    }

                    // verify the trigger type
                    if (trConfig.TriggerType != TriggerTypes.OnTagChange && trConfig.TriggerType != TriggerTypes.OnCycle)
                    {
                        var ex = new Exception($"Invalid TriggerType. TriggerType ({trConfig.TriggerType }) Transaction ({trConfig.Name})");
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

                        var compatModes = (dvTag.Mode == Modes.ToOTM && dpParam.Mode == Modes.FromOTM) ||
                                          (dvTag.Mode == Modes.FromOTM && dpParam.Mode == Modes.ToOTM);
                        if (!compatModes)
                        {
                            var ex = new Exception($"Invalid modes between DataPointParam and DeviceTag in Transaction config (valid is in => out or out => in). DataPointParam ({bind.DataPointParam}) DeviceTag ({bind.DeviceTag}) Transaction ({trConfig.Name})");
                            ex.Data.Add("field", "Name");
                            throw ex;
                        }

                        if (dvTag.TypeCode != dpParam.TypeCode)
                        {
                            var ex = new Exception($"Invalid data type between DataPointParam and DeviceTag in Transaction config. DataPointParam ({bind.DataPointParam}) DeviceTag ({bind.DeviceTag}) Transaction ({trConfig.Name})");
                            ex.Data.Add("field", "Name");
                            throw ex;
                        }
                    }

                    var transaction = new Transaction(trConfig, devices[trConfig.DeviceName], dataPoints[trConfig.DataPointName], logger);
                    transactions[trConfig.Name] = transaction;

                    logger.LogError($"Transaction {trConfig.Name}: Created");
                }

            return transactions;
        }
    }
}