namespace Otm.Server.Device.Licensing
{
    public class LicensingRequest
    {
        public LicensingRequest(string hostIdentifier, string deviceIdentifier, string key)
        {
            HostIdentifier = hostIdentifier;
            DeviceIdentifier = deviceIdentifier;
            Key = key;
        }

        public string HostIdentifier { get; private set; } // Must be Automatic
        public string DeviceIdentifier { get; private set; } //Must be automatic
        public string Key { get; private set; } //Must be parameter
    }
}
