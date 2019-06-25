using System;
using System.Collections.Generic;
using NLog;
using Otm.Config;
using Otm.DataPoint;
using Otm.Device;
using Otm.Logger;

namespace Otm.Transaction
{
    public interface ITransactionFactory
    {
        IDictionary<string, ITransaction> CreateTransactions(IEnumerable<TransactionConfig> config, 
        IDictionary<string, IDataPoint> dataPoints, IDictionary<string, IDevice> devices, ILoggerFactory loggerFactory);
    }
}