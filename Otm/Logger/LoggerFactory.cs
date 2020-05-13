
using System.Runtime.CompilerServices;
using NLog;

namespace Otm.Logger
{
    public static class LoggerFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILogger GetCurrentClassLogger() 
        {
            return LogManager.GetCurrentClassLogger();
        }
    }
}