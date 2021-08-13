using Otm.Shared.ContextConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otm.Server.ContextConfig
{
    public interface IDeviceService
    {
        void CreateOrEditDevice(DeviceConfig device);

        void DeleteDevice(DeviceInput input);

    }
}
