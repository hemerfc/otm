using System;
using System.Linq;
using Xunit;
using Otm.DataPoint;
using Otm.Config;
using System.Collections.Generic;
using Moq;
using Otm.Logger;
using NLog;

namespace Otm.Test.DataPoint
{
    public class MsSqlDataPointTests
    {
        [Fact]
        public void Execute_DataPoint_sp_test01()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{ 
                new DataPointConfig {
                    Name = "[dbo].[sp_test01]",
                    Driver = "mssql",
                    Config = "Server=localhost\\SQLEXPRESS;Database=OTM;User Id=otm;Password=otm;",
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {  
                            Name = "@p1",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.FromOTM
                        },
                        new DataPointParamConfig {  
                            Name = "@p2",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.ToOTM
                        }
                    }).ToList()
                }
            };
            
            var inParams = new Dictionary<string, object>();
            inParams["@p1"] = 2;
            inParams["@p2"] = 0;

            var dpSpTest01 = new DataPointFactory().CreateDataPoints(dpConfig)["[dbo].[sp_test01]"];

            var outParams = dpSpTest01.Execute(inParams);
            
            Assert.Equal(4, outParams["@p2"]);
        }


        [Fact]
        public void Execute_DataPoint_sp_test01_2()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{ 
                new DataPointConfig {
                    Name = "[dbo].[sp_test01]",
                    Driver = "mssql",
                    Config = "Server=localhost\\SQLEXPRESS;Database=OTM;User Id=otm;Password=otm;",
                    Params = (new DataPointParamConfig[] {
                        new DataPointParamConfig {  
                            Name = "@p1",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.FromOTM
                        },
                        new DataPointParamConfig {  
                            Name = "@p2",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.ToOTM
                        },
                        new DataPointParamConfig {  
                            Name = "@p3",
                            TypeCode = TypeCode.Int32,
                            Mode = Modes.Static
                        }
                    }).ToList()
                }
            };
            
            var inParams = new Dictionary<string, object>();
            inParams["@p1"] = 10;
            inParams["@p2"] = 0;
            inParams["@p3"] = 0;

            var dpSpTest01 = new DataPointFactory().CreateDataPoints(dpConfig)["[dbo].[sp_test01]"];

            var outParams = dpSpTest01.Execute(inParams);
            
            Assert.Equal(100, outParams["@p2"]);
        }
    }
}
