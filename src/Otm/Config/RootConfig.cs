using System;

namespace Otm.Config
{
    public class RootConfig
    {
        public string Name { get; set; }
        public DataPointConfig[] DataPoints { get; set; }
        public DeviceConfig[] Devices { get; set; }
        public TransactionConfig[] Transactions { get; set; }

        public static RootConfig GetDefaultConfig()
        {
            return new RootConfig{
                Name = "AppName",
                DataPoints = new DataPointConfig[] {
                    new DataPointConfig { 
                        Name = "dp01", 
                        Driver = "odbc",
                        Config = "dsn=DB01", 
                        Params = new DataPointParamConfig[] {
                            new DataPointParamConfig  { 
                                Name = "p1",
                                Type = "int",
                                Mode = "in"
                            }                          
                        }
                    }
                },
                Devices = new DeviceConfig[] {
                    new DeviceConfig {
                        Name = "plc01",
                        Driver = "snap7",
                        Config = "host=192.168.1.1;rack=0;slot=0",
                        Tags = new DeviceTagConfig[] {
                            new DeviceTagConfig {
                                Name = "",
                                Address = "",
                                Rate = 50,
                                Mode = ""
                            }
                        } 
                    }
                },
                Transactions = new TransactionConfig[] {
                    new TransactionConfig {
                        Name = "tr01",
                        TriggerType = "on_change",
                        TriggerTagName = "",
                        DataPointName = "",
                        Binds = new TransactionBindConfig[] { 
                            new TransactionBindConfig {
                                DeviceTag = "",
                                DataPointParam = "",
                                Value = ""
                            }
                        }
                    }
                }                    
            };
        }        
    }
}