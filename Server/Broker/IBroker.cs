
using System;
using System.ComponentModel;
using NLog;
using Otm.Server.ContextConfig;

namespace Otm.Server.Broker
{
    public interface IBroker : IBrokerStatus
    { 
        void Init(BrokerConfig config, ILogger logger);
    
        void Start(BackgroundWorker worker);
        void Stop();

        bool Ready { get; }
        BackgroundWorker Worker { get;  }
    }
}