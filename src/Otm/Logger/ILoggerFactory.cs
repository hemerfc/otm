using NLog;

namespace Otm.Logger
{
    public interface ILoggerFactory
    {
        ILogger GetCurrentClassLogger();
    }
}