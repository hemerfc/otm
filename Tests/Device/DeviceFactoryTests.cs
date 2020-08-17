using System;
using Xunit;
using Moq;
using Otm.Server.Device;
using Otm.Shared.ContextConfig;
using Microsoft.Extensions.Logging;
using Otm.Server.Device.S7;
using System.Collections.Generic;

namespace Otm.Test.Device
{
    public class DeviceFactoryTests
    {
        [Fact]
        public void InvalidDeviceName()
        {
            // prepare
            var dvConfig = new List<DeviceConfig>{
                new DeviceConfig {
                    Name = "",
                    Driver = "",
                    Config = "",
                    Tags = null
                }
            };

            var loggerMock = new Mock<ILogger>();
            var ex = Record.Exception(() => DeviceFactory.CreateDevices(dvConfig, loggerMock.Object));

            Assert.Equal("Name", ex?.Data["field"]);
        }

        [Fact]
        public void InvalidDriverName()
        {
            // prepare
            var dpConfig = new List<DeviceConfig>{
                new DeviceConfig {
                    Name = "plc01",
                    Driver = "xxx",
                    Config = "",
                    Tags = null
                }
            };

            var loggerMock = new Mock<ILogger>();
            var ex = Record.Exception(() => DeviceFactory.CreateDevices(dpConfig, loggerMock.Object));
            Assert.Equal("Driver", ex?.Data["field"]);
        }

        [Fact]
        public void CreateS7Device()
        {
            // prepare
            var dpConfig = new List<DeviceConfig>{
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

            var loggerMock = new Mock<ILogger>();
            var devices = DeviceFactory.CreateDevices(dpConfig, loggerMock.Object);

            Assert.Equal(2, devices.Count);

            Assert.IsType<S7Device>(devices["plc01"]);

            Assert.IsType<S7Device>(devices["plc02"]);
        }
    }
}
