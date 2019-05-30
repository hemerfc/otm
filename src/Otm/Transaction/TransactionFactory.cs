using System;
using System.Collections.Generic;
using NLog;
using Otm.Config;
using Otm.DataPoint;
using Otm.Device;

namespace Otm.Transaction
{
    public class TransactionFactory : ITransactionFactory
    {
        public IDictionary<string, ITransaction> CreateTransactions(IEnumerable<TransactionConfig> config, IDictionary<string, IDataPoint> dataPoints, IDictionary<string, IDevice> devices)
        {
            throw new NotImplementedException();
        }
    }
}