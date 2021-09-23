using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Server.Device.RabbitMq
{
    public class RabbitMessage
    {
        public Guid Id { get => id; set => id = value; }
        public int Cod { get => cod; set => cod = value; }

        public Guid id;
        public int cod;
    }
}
