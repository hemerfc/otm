
namespace Otm.Transaction
{
    public interface ITransaction
    {
        void Start();
        
        void Stop();
    }
}