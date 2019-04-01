
using System.Runtime.CompilerServices;
using NLog;

namespace Otm.Logger
{
    public class LoggerFactory : ILoggerFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ILogger GetCurrentClassLogger() 
        {
            return LogManager.GetCurrentClassLogger();
        }
    }
}