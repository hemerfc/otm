namespace Otm.Server.Device.Licensing
{
    public class LicensingResponse
    {
        public bool isValid { get; set; }
        public int remainingHours { get; set; }
        public string message { get; set; }
    }
}
