using System;
using System.Linq;
using Xunit;
using Otm.Config;
using Otm.Device;
using Moq;
using Otm.Logger;
using NLog;
using Otm.Device.S7;

namespace Otm.Test.Device
{
    public class S7DeviceTests
    {
        [Fact]
        public void Update_Device_Tags()
        {
            // prepare
            var dvConfig = new DeviceConfig
            {
                Name = "plc01",
                Driver = "s7",
                Config = "host=127.0.0.1;rack=0;slot=0",
                Tags = (new DeviceTagConfig[] 
                {
                    new DeviceTagConfig
                    {  
                        Name = "tag1",
                        TypeCode = TypeCode.Int32,
                        Mode = Modes.FromOTM, // write to plc
                        Address = "db801.dw0",
                        Rate = 50                            
                    },
                    new DeviceTagConfig
                    {
                        Name = "tag2",
                        TypeCode = TypeCode.Int32,
                        Mode = Modes.ToOTM, // read from plc
                        Address = "db800.dw12",
                        Rate = 50
                    }
                }).ToList()
            };

            var buffer = new byte[13];
            byte dw10 = 99;

            var clientMock = new Mock<IS7Client>();
            // on the first call conected will be False
            clientMock.Setup(x => x.Connected).Returns(true);

            clientMock.Setup(x => x.ConnectTo("127.0.01", 0, 0))
                       .Returns(0);

            clientMock.Setup(x => x.ErrorText(0)).Returns("No error");
            clientMock.Setup(x => x.DBRead(800, 0, 16, It.IsAny<byte[]>()))
                      .Callback<int, int, int, byte[]>((dbNumber, start, lenght, buf) => 
                      {
                          // change second byte of dw13
                           buf[13] = dw10;
                      });

            var factoryMock = new Mock<IS7ClientFactory>();
            factoryMock.Setup(x => x.CreateClient()).Returns(clientMock.Object);

            var devPlc01 = new S7Device(dvConfig, factoryMock.Object);

            devPlc01.ReadDeviceTags();
            
            var tag2 = -1;
            // update tag value to 0
            dw10 = 1;
            // assign a function to be executed when tag value is changed
            devPlc01.OnTagChangeAdd("tag2", (str, value) => {
                tag2 = (int)value;
            });

            devPlc01.ReadDeviceTags();

            Assert.Equal(1, tag2);

            // update tag value to 1
            dw10 = 2;

            devPlc01.ReadDeviceTags();

            Assert.Equal(2, tag2);
        }
    }
}

