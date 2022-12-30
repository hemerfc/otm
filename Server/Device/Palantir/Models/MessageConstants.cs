using System;

namespace Otm.Server.Device.Palantir.Models
{
    public struct MessageConstants
    {
        public const byte STX = 0x02; // Start
        public const byte ETX = 0x03; // End
        public const string DATETIME_PATTERN = "yyyy-MM-ddTHH:mm:ss.fff";

        public const UInt16 K01_INTERVAL_MS = 5000;
        public const UInt16 K01_TIMEOUT_MS = 10000;
    }
}
