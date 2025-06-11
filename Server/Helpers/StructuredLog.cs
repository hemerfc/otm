using System;
using NLog;
using NLog.Fluent;

namespace Otm.Server.Helpers
{
    public class StructuredLog
    {
        public StructuredLog(
            LogObject logObject,
            string className,
            string methodName,
            string config,
            string loggerName,
            string device,
            string value = null)
        {
            if (logObject.LogType == LogType.Error && string.IsNullOrWhiteSpace(logObject.Codigo))
                throw new ArgumentException("Erro Code é obrigatório para logs do tipo Error.");

            Type = logObject.LogType;
            ClassName = className;
            MethodName = methodName;
            Config = config;
            LoggerName = loggerName;
            Device = device;
            Message = logObject.Mensagem;
            ErrorCode = Type == LogType.Info ? string.Empty : logObject.Codigo;
            Timestamp = DateTime.Now;
            Value = value ?? string.Empty;
        }

        public LogType Type { get; }
        public string ClassName { get; }
        public string MethodName { get; }
        public string Config { get; }
        public string LoggerName { get; }
        public string Device { get; }
        public string Message { get; }
        public string ErrorCode { get; }
        public DateTime Timestamp { get; }

        public String Value { get; }

        public void Write()
        {
            var logger = LogManager.GetLogger(LoggerName);

            var logEvent = new LogEventInfo
            {
                Level = Type switch
                {
                    LogType.Info => LogLevel.Info,
                    LogType.Error => LogLevel.Error,
                    LogType.Warning => LogLevel.Warn,
                    LogType.Debug => LogLevel.Debug,
                    _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "Tipo de log não suportado.")
                },
                Message = Message
            };

            logEvent.Properties["Class"] = ClassName;
            logEvent.Properties["Method"] = MethodName;
            logEvent.Properties["Config"] = Config;
            logEvent.Properties["Device"] = Device;
            logEvent.Properties["ErrorCode"] = ErrorCode;
            logEvent.Properties["Timestamp"] = Timestamp;
            logEvent.Properties["Value"] = Value;

            logger.Log(logEvent);
        }
    }
}