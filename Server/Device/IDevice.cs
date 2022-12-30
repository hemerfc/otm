
using System;
using System.ComponentModel;
using NLog;
using Otm.Server.ContextConfig;
using Otm.Shared.ContextConfig;
using Otm.Shared.Status;

namespace Otm.Server.Device
{
    public interface IDevice : IDeviceStatus
    { 
        void Init(DeviceConfig dvConfig, ILogger logger);
    
        void OnTagChangeAdd(string tagName, Action<string, object> triggerAction);

        void OnTagChangeRemove(string tagName, Action<string, object> triggerAction);

        bool ContainsTag(string tagName);

        DeviceTagConfig GetTagConfig(string name);
        object GetTagValue(string tagName);
        void SetTagValue(string tagName, object value);
        void Start(BackgroundWorker worker);
        void Stop();

        bool Ready { get; }
        BackgroundWorker Worker { get;  }

        #region License
        //void GetLicenseRemainingHours();
        //int LicenseRemainingHours { get; set; }
        //DateTime? LastUpdateDate { get; set; }

        #endregion
    }
}