using System;
using System.ComponentModel;
using NLog;

namespace Otm.Components
{
    public interface IBaseComponent
    {
        void Init(String componentConfig, ILogger logger, IMessager messager);

        void Start(BackgroundWorker worker);

        void Stop();
    }
}