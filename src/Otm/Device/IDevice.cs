
using System;

namespace Otm.Device
{
    public interface IDevice
    {
        void UpdateTags();

        void OnTagChange(string tagName, Action<string, object> triggerAction);
    }
}

