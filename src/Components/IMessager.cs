
namespace Otm.Components
{    
    public interface IMessager
    {
        void SendMessage(string name, string source, string target, object data);
    }
}