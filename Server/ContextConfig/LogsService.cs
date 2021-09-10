using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Otm.Server.ContextConfig
{
    public class LogsService : ILogsService
    {

        public List<string> GetFilesName()
        {
            string[] arquivos = Directory.GetFiles("Logs", "*.json", SearchOption.AllDirectories);
            List<string> arquives = new List<string>();

            foreach (var File in arquivos)
            {
                arquives.Add(File);
            }
            return arquives;
        }
    }
}
