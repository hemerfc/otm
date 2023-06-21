using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Server.ContextConfig
{
    public class TransactionInput
    {
        public Guid Id { get; set; }
        public string name { get; set; }
        public string ContextName { get; set; }
    }
}
