using System;
using System.Collections.Generic;

namespace Otm.Server.ContextConfig
{
    public class BrokerConfig
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Driver { get; set; }

        public string SocketHostName { get; set; }
        public int SocketPort { get; set; }
        public string MasterDevice { get; set; }
        public string PtlId { get; set; }
        public string AmqpHostName { get; set; }
        public int AmqpPort { get; set; }
        // filas que devem ser consumidas e encaminhadas oara o socket
        // os nomes devem estar separado por | ex: "PLC01_K02|PLC01_R02"
        public string AmqpQueueToConsume { get; set; }
        public string AmqpQueueToProduce { get; set; }

        public List<BrokerMessageTypeConfig> MessageTypes { get; set; }
        public string ContextName { get; set; }


    }
}