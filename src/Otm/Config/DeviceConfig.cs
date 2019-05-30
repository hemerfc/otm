namespace Otm.Config
{
    public class DeviceConfig
    {
        public string Name { get; set; }
        public string Driver { get; set; }
        public string Config { get; set; }
        public DeviceTagConfig[] Tags { get; set; }
    }
}