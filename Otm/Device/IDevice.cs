
using System;
using System.ComponentModel;
using Otm.ContextConfig;

namespace Otm.Device
{
    public interface IDevice
    {
        string Name { get; }
        BackgroundWorker Worker { get; }

        void ReadDeviceTags();

        void OnTagChangeAdd(string tagName, Action<string, object> triggerAction);

        void OnTagChangeRemove(string tagName, Action<string, object> triggerAction);

        bool ContainsTag(string tagName);

        DeviceTagConfig GetTagConfig(string name);
        object GetTagValue(string tagName);
        void SetTagValue(string tagName, object value);
        void Start(BackgroundWorker worker);
        void Stop();
    }
}

