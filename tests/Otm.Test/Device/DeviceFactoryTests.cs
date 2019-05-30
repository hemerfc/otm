using System;
using Xunit;
using Otm.DataPoint;
using Otm.Config;
using Otm.Device;

namespace Otm.Test.Device
{
    public class DeviceFactoryTests
    {
        [Fact]
        public void InvalidDeviceName()
        {
            // prepare
            var dvConfig = new DeviceConfig[]{ 
                new DeviceConfig {
                    Name = "",
                    Driver = "",
                    Config = "",
                    Tags = null
                }
            };
        
            var ex = Record.Exception(() => new DeviceFactory().CreateDevices(dvConfig));

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

            var ex = Record.Exception(() => new DataPointFactory().CreateDataPoints(dpConfig));

            Assert.Equal("Driver", ex?.Data["field"]);
        }

        [Fact]
        public void CreateOdbcDataPoint()
        {
            // prepare
            var dpConfig = new DataPointConfig[]{ 
                new DataPointConfig {
                    Name = "dp01",
                    Driver = "odbc",
                    Config = "dsn=DB01",
                    Params = null
                },
                new DataPointConfig {
                    Name = "dp02",
                    Driver = "odbc",
                    Config = "dsn=DB02",
                    Params = null
                }
            };
        
            var factory = new DataPointFactory();

            var datapoints = factory.CreateDataPoints(dpConfig);

            Assert.Equal(2, datapoints.Count);

            Assert.IsType<OdbcDataPoint>(datapoints["dp01"]);

            Assert.IsType<OdbcDataPoint>(datapoints["dp02"]);
        }
    }
}
