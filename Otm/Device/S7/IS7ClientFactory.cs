using System;
using System.Collections.Generic;
using System.Text;

namespace Otm.Device.S7
{
    public interface IS7ClientFactory
    {
        IS7Client CreateClient();
    }
}
