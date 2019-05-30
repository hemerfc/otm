namespace Otm.Config
{
    public class TransactionConfig
    {
        public string Name { get; set; }
        public string TriggerType { get; set; }
        public string TriggerTagName { get; set; }
        public string DataPointName { get; set; }
        public TransactionBindConfig[] Binds { get; set; }
    }
}