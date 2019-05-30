namespace Otm.Config
{
    public class DataPointConfig
    {
        public string Name { get; set; }
        public string Driver { get; set; }
        public string Config { get; set; }

        public DataPointParamConfig[] Params { get; set; }
    }
}