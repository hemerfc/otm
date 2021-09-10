using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Server.ContextConfig
{

    public interface ILogsService
    {
        List<string> GetFilesName();

    }
}
