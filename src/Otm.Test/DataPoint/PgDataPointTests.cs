using System;
using Xunit;
using Otm.DataPoint;
using Otm.Config;
using System.Collections.Generic;
using Moq;
using Otm.Logger;
using NLog;

namespace Otm.Test.DataPoint
{
    public class PgDataPointTests
    {
        [Fact]
        public void Execute_DataPoint_sp_test01()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{ 
                new DataPointConfig {
                    Name = "sp_test01",
                    Driver = "pg",
                    Config = "Host=localhost;Database=otm_test;Username=otm;Password=otm",
                    Params = new DataPointParamConfig[] {
                        new DataPointParamConfig {  
                            Name = "p1",
                            Type = "int",
                            Mode = "in"
                        },
                        new DataPointParamConfig {  
                            Name = "p2",
                            Type = "int",
                            Mode = "out"
                        }
                    }
                }
            };
            
            var inParams = new Dictionary<string, object>();
            inParams["p1"] = 1;
            inParams["p2"] = 0;
        
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.GetCurrentClassLogger()).Returns(new Mock<ILogger>().Object);
            
            var dpSpTest01 = new DataPointFactory().CreateDataPoints(dpConfig, loggerFactoryMock.Object)["sp_test01"];

            var outParams = dpSpTest01.Execute(inParams);
            
            Assert.Equal(2, outParams["p2"]);
        }
    }
}
