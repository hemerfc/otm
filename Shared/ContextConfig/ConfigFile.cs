using System;
using System.Collections.Generic;
using System.Text;

namespace Otm.Shared.ContextConfig
{
    public record ConfigFile
    {
        public string Name { get; init; }

        public string Path { get; init; }

        public DateTime ModifiedAt { get; init; }

    }
}
