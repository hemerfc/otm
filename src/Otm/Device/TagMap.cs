using System.Collections.Concurrent;

namespace Otm.DeviceDrivers
{
    /// <summary>
    /// Provides a thread-safe dictionary for use with data binding.
    /// </summary>
    public class TagMap
    {
        private ConcurrentDictionary<string, Tag> tagDict;

        public object this[string index]
        {
            get
            {
                return tagDict[index];
            }
            set
            {
                // se é uma nova chave, cria a tag
                if (!tagDict.TryGetValue(index, out var itemValue))
                {
                    if (itemValue != value) {
                        tagDict[index] = new Tag(index, value);
                    }                        
                }
                else 
                {
                    // se é um novo valor para a tag, atualiza o valor
                    if (itemValue != value){
                        tagDict[index].Value = value;
                    }
                }
            }
        }

        public TagMap()
        {
            tagDict = new ConcurrentDictionary<string,Tag>();
        }
    }
}