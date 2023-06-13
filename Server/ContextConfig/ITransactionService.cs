using Otm.Server.ContextConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Server.ContextConfig
{
    public interface ITransactionService
    {
        void CreateOrEditTransaction(TransactionConfig device);
        void DeleteTransaction(TransactionInput input);
    }
}
