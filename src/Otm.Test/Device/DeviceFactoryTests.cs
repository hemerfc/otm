using System;
using Xunit;
using Otm.Device;
using Otm.Config;
using Moq;
using Otm.Logger;

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
            var loggerFactory = new Mock<ILoggerFactory>();
            
            var ex = Record.Exception(() => new DeviceFactory().CreateDevices(dvConfig, loggerFactory.Object));

            Assert.Equal("Name", ex?.Data["field"]);
        }

        [Fact]
        public void InvalidDriverName()
        {
            // prepare
            var dpConfig = new DeviceConfig[]{ 
                new DeviceConfig {
                    Name = "plc01",
                    Driver = "xxx",
                    Config = "",
                    Tags = null
                }
            };

            var loggerFactory = new Mock<ILoggerFactory>();
            var ex = Record.Exception(() => new DeviceFactory().CreateDevices(dpConfig, loggerFactory.Object));
            Assert.Equal("Driver", ex?.Data["field"]);
        }

        [Fact]
        public void CreateS7Device()
        {
            // prepare
            var dpConfig = new DeviceConfig[]{ 
                new DeviceConfig {
                    Name = "plc01",
                    Driver = "s7",
                    Config = "host=127.0.0.1;rack=0;slot=0",
                    Tags = null
                },
                new DeviceConfig {
                    Name = "plc02",
                    Driver = "s7",
                    Config = "host=127.0.0.1;rack=0;slot=0",
                    Tags = null
                }
            };
        
            var factory = new DeviceFactory();

            var loggerFactory = new Mock<ILoggerFactory>();
            var devices = factory.CreateDevices(dpConfig, loggerFactory.Object);

            Assert.Equal(2, devices.Count);

            Assert.IsType<S7Device>(devices["plc01"]);

            Assert.IsType<S7Device>(devices["plc02"]);
        }
    }
}
