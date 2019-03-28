
namespace Otm.Components
{    
    public class ComponentMessage
    {
        public string Name { get; }
        public string Source { get; }
        public string Target { get; }
        public object Data { get; }

        public ComponentMessage(string name, string source, string target, object data=null)
        {
            Name = name;
            Source = source;
            Target = target;
            Data = data;
        }
    }
}