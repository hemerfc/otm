
using System.ComponentModel;

namespace Otm.Transaction
{
    public interface ITransaction
    {
        BackgroundWorker Worker { get; }
        string Name { get; }

        void Start(BackgroundWorker worker);
        void Stop();
    }
}