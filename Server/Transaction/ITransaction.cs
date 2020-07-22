
using System.ComponentModel;

namespace Otm.Server.Transaction
{
    public interface ITransaction
    {
        BackgroundWorker Worker { get; }
        string Name { get; }

        void Start(BackgroundWorker worker);
        void Stop();
    }
}