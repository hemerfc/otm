using Otm.Config;

namespace Otm.Device
{
    public class S7Device : IDevice
    {
        private DeviceConfig dvConfig;

        public S7Device(DeviceConfig dvConfig)
        {
            this.dvConfig = dvConfig;
        }
    }
}