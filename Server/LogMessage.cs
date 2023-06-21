using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Server
{
    public record LogMessage(Guid Id, DateTime DateTime, string Level, string Origin, string Message);
}
