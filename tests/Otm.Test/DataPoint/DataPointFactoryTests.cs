using System;
using Xunit;
using Otm.DataPoint;
using Otm.Config;
using Moq;
using Otm.Logger;
using NLog;

namespace Otm.Test.DataPoint
{
    public class DataPointFactoriesTests
    {
        [Fact]
        public void InvalidDataPointName()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{ 
                new DataPointConfig {
                    Name = "",
                    Driver = "",
                    Config = "",
                    Params = null
                }
            };
        
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.GetCurrentClassLogger()).Returns(new Mock<ILogger>().Object);

            var ex = Record.Exception(() => new DataPointFactory().CreateDataPoints(dpConfig, loggerFactoryMock.Object));

            Assert.Equal("Name", ex?.Data["field"]);
        }

        [Fact]
        public void InvalidDriverName()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{ 
                new DataPointConfig {
                    Name = "dp01",
                    Driver = "",
                    Config = "",
                    Params = null
                }
            };        

            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.GetCurrentClassLogger()).Returns(new Mock<ILogger>().Object);

            var ex = Record.Exception(() => new DataPointFactory().CreateDataPoints(dpConfig, loggerFactoryMock.Object));

            Assert.Equal("Driver", ex?.Data["field"]);
        }

        [Fact]
        public void CreateOdbcDataPoint()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{ 
                new DataPointConfig {
                    Name = "dp01",
                    Driver = "pg",
                    Config = "dsn=DB01",
                    Params = null
                },
                new DataPointConfig {
                    Name = "dp02",
                    Driver = "pg",
                    Config = "dsn=DB02",
                    Params = null
                }
            };
        
            var factory = new DataPointFactory();

            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.GetCurrentClassLogger()).Returns(new Mock<ILogger>().Object);

            var datapoints = factory.CreateDataPoints(dpConfig, loggerFactoryMock.Object);

            Assert.Equal(2, datapoints.Count);

            Assert.IsType<PgDataPoint>(datapoints["dp01"]);

            Assert.IsType<PgDataPoint>(datapoints["dp02"]);
        }
    }
}
