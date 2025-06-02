namespace Otm.Server.Helpers
{
    public static class ErrorCodes
    {
        public static class RabbitMq
        {
            public const string ConnectionError = "EX-CN-RBT";
            public const string ChannelNull = "EX-RBT-NULL";
            public const string ChannelDisconnected = "EX-RBT-DISC";
        }

        public static class Ptl
        {
            public const string ConnectionError = "EX-CN-PTL";
            public const string KeepAliveTimeout = "EX-PTL-KA-TO";
        }

        public static class Plc
        {
            public const string ConnectionError = "EX-CN-PLC";
            public const string ClientNull = "EX-PLC-NULL";
            public const string ClientDisconnected = "EX-PLC-DISC";
            public const string KeepAliveTimeout = "EX-PLC-KA-TO";
            public const string SendDataNullItem = "EX-PLC-SEND-NULL";
        }
    }
}