using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Server.Device.RabbitMq
{
    public class RabbitMessage
    {
        public Guid Id { get; set; }
        public string Cod { get; set; }
    }
}
