﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Server.ContextConfig
{
    public class DeviceInput
    {
        public Guid Id { get; set; }
        public string name { get; set; }
        public string ContextName { get; set; }
        public string host { get; set; }
        public int rack { get; set; }
        public int slot { get; set; }
    }
}
