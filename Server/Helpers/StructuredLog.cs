using System;
using Microsoft.Extensions.Logging;

namespace Otm.Server.Helpers
{
    public class StructuredLog
    {
        private readonly ILogger _logger;

        public StructuredLog(
            ILogger logger,
            LogObject logObject,
            string className,
            string methodName,
            string config,
            string device)
        {
            if (logObject.LogType == LogType.Error && string.IsNullOrWhiteSpace(logObject.Codigo))
                throw new ArgumentException("Erro Code é obrigatório para logs do tipo Error.");

            Type = logObject.LogType;
            ClassName = className;
            MethodName = methodName;
            Config = config;
            Device = device;
            Message = logObject.Mensagem;
            ErrorCode = Type == LogType.Info ? string.Empty : logObject.Codigo;
            Timestamp = DateTime.Now;

            _logger = logger;
        }

        public LogType Type { get; }
        public string ClassName { get; }
        public string MethodName { get; }
        public string Config { get; }
        public string Device { get; }
        public string Message { get; }
        public string ErrorCode { get; }
        public DateTime Timestamp { get; }

        public void Write()
        {
            var logEntry = new
            {
                Class = ClassName,
                Method = MethodName,
                Config,
                Device,
                ErroCode = ErrorCode,
                Message,
                Timestamp
            };

            if (Type == LogType.Info)
                _logger.LogInformation("{@Log}", logEntry);
            else if (Type == LogType.Error)
                _logger.LogError("{@Log}", logEntry);
            else if (Type == LogType.Warning)
                _logger.LogWarning("{@Log}", logEntry);
            else if (Type == LogType.Debug)
                _logger.LogDebug("{@Log}", logEntry);
            else
                throw new ArgumentOutOfRangeException(nameof(Type), Type, "Tipo de log não suportado.");
        }
    }
}