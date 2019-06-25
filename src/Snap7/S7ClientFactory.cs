using System;
using System.Collections.Generic;
using System.Text;

namespace Snap7
{
    public class S7ClientFactory : IS7ClientFactory
    {
        public IS7Client CreateClient()
        {
            return new S7Client();
        }
    }
}
