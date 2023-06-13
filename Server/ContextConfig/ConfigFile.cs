using System;
using System.Collections.Generic;
using System.Text;

namespace Otm.Server.ContextConfig
{
    public record ConfigFile
    {
        public string Name { get; init; }
        public string Mode { get; init; }

        public string Path { get; init; }

        public DateTime ModifiedAt { get; init; }

        public bool Status { get; init; }

    }
}
