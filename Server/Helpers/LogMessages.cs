namespace Otm.Server.Helpers
{
    public static class LogMessages
    {
        public static class RabbitMq
        {
            public static readonly LogObject Connected =
                new(string.Empty, "RabbitMQ successfully connected.", LogType.Info);

            public static readonly LogObject Disconnected =
                new(ErrorCodes.RabbitMq.ConnectionError, "Failed to connect to Rabbit.", LogType.Error);

            public static readonly LogObject NotReady =
                new(string.Empty, "RabbitMQ not ready.", LogType.Info);

            public static readonly LogObject ChannelNull =
                new(ErrorCodes.RabbitMq.ChannelNull, "AMQP Channel is null.", LogType.Error);

            public static readonly LogObject ChannelDisconnected =
                new(ErrorCodes.RabbitMq.ChannelDisconnected, "AMQP Channel is not connected.", LogType.Error);
        }

        public static class Ptl
        {
            public static readonly LogObject Connected =
                new(string.Empty, "PTL successfully connected.", LogType.Info);

            public static readonly LogObject Disconnected =
                new(ErrorCodes.Ptl.ConnectionError, "Failed to connect to PTL.", LogType.Error);

            public static readonly LogObject Reconnected =
                new(string.Empty, "Reconnecting to PTL.", LogType.Info);

            public static class KeepAlive
            {
                public static readonly LogObject Timeout =
                    new(ErrorCodes.Ptl.KeepAliveTimeout, "Keep alive timeout (PTL).", LogType.Error);

                public static readonly LogObject Loop =
                    new(string.Empty, "Keep alive loop (PTL).", LogType.Info);
            }
        }

        public static class Plc
        {
            public static readonly LogObject Connected =
                new(string.Empty, "PLC successfully connected.", LogType.Info);

            public static readonly LogObject Disconnected =
                new(ErrorCodes.Plc.ConnectionError, "Failed to connect to PLC.", LogType.Error);

            public static readonly LogObject Reconnected =
                new(string.Empty, "Reconnecting to PLC.", LogType.Info);

            public static readonly LogObject NotReady =
                new(string.Empty, "PLC not ready.", LogType.Info);

            public static readonly LogObject ClientNull =
                new(ErrorCodes.Plc.ClientNull, "PLC client is null.", LogType.Error);

            public static readonly LogObject ClientDisconnected =
                new(ErrorCodes.Plc.ClientDisconnected, "PLC client is not connected.", LogType.Error);

            public static class KeepAlive
            {
                public static readonly LogObject Timeout =
                    new(ErrorCodes.Plc.KeepAliveTimeout, "KEEP_ALIVE_TIMEOUT. Setting PlcReady to false...",
                        LogType.Info);

                public static readonly LogObject Loop =
                    new(string.Empty, "Keep alive loop (PLC).", LogType.Info);
            }

            public static class ReceiveData
            {
                public static readonly LogObject MessagePublished =
                    new(string.Empty, "Message received and published to RabbitMQ.", LogType.Info);
            }

            public static class SendData
            {
                public static readonly LogObject NullItem =
                    new(ErrorCodes.Plc.SendDataNullItem, "SendData(): Null item in queue.", LogType.Error);

                public static readonly LogObject TotalLength =
                    new(string.Empty, "SendData(): Total length calculated.", LogType.Info);

                public static readonly LogObject MessageJson =
                    new(string.Empty, "SendData(): Message JSON extracted.", LogType.Info);

                public static readonly LogObject MatchCollection =
                    new(string.Empty, "SendData(): Regex matches extracted.", LogType.Info);

                public static readonly LogObject MatchItem =
                    new(string.Empty, "SendData(): Message body match processed.", LogType.Info);

                public static readonly LogObject ClientConnection =
                    new(string.Empty, "SendData(): Client connection status.", LogType.Info);

                public static readonly LogObject MessageSent =
                    new(string.Empty, "SendData(): Message sent to device.", LogType.Info);
            }

            public static readonly LogObject ConnectionAttempt =
                new(string.Empty, "Attempting to connect to PLC.", LogType.Info);

            public static readonly LogObject ConnectionFailed =
                new(ErrorCodes.Plc.ConnectionError, "Failed to connect to PLC.", LogType.Error);
        }
    }
}