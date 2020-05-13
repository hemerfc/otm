using System;
using System.Collections.Generic;
using System.Text;

namespace Otm.DataPoint
{
    public class DataPointFunction
    {
        public string Name { get; set; }

        // name and type
        public Dictionary<string, TypeCode> Parans { get; set; }
    }
}
