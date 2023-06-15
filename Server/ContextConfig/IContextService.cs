using Otm.Server.ContextConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Server.ContextConfig
{
    public interface IContextService
    {
        void CreateOrEditContext(ContextInput Context);
        void DeleteContext(ContextInput input);
    }
}
