
using System;
using Otm.Config;

namespace Otm.Device
{
    public interface IDevice
    {
        void UpdateTags();

        void OnTagChangeAdd(string tagName, Action<string, object> triggerAction);

        void OnTagChangeRemove(string tagName, Action<string, object> triggerAction);

        bool ContainsTag(string tagName);
        
        DeviceTagConfig GetTagConfig(string name);
        object GetTagValue(string tagName);
        void SetTagValue(string tagName, object value);
    }
}

