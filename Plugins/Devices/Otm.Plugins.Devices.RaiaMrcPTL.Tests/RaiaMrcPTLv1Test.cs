using Moq;
using NLog;
using Otm.Server.Device.Ptl;
using Otm.Server.ContextConfig;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Otm.Plugins.Devices.RaiaMrcPTL.Tests
{
    public class RaiaMrcPTLv1Test
    {
        byte[] recvFromPtlBuffer = new byte[0];
        byte[] sendToPtlBuffer = new byte[0];

        [Fact]
        public void Send_Msg01()
        {
            /*
              ITEMSTATUS^OLPN^TC_ORDER_ID^ALLOC_INVN_ID^ITEM_NAME^STATE^SERIAL_NUMBER^BATCH_NUMBER
              ^INITIAL_QUANTITY^PACKED_QUANTITY^XREF_OLPN^LPN_DETAIL_ID^GRP_ATTR
            */
            var cmd1 = $"ITEMSTATUS^0000002774873 ^31033241 ^191919101 ^24811 ^COMPLETE^ ^CBZ7E058 ^2 ^2 ^287837 ^40788636 ^S08";
            var cmd2 = $"ITEMSTATUS^0000002774873 ^31033241 ^191919101 ^24811 ^COMPLETE^ ^CBZ7E058 ^2 ^2 ^287837 ^40788636 ^S09";

            recvFromPtlBuffer = Array.Empty<byte>();
            sendToPtlBuffer = Array.Empty<byte>();

            var devPtl = CreateDevice(out BackgroundWorker bgWorker);

            // inicia o loop do device
            bgWorker.RunWorkerAsync();
            Thread.Sleep(50);

            devPtl.SetTagValue("cmd_send", cmd1);

            devPtl.SetTagValue("cmd_send", cmd2);
            Thread.Sleep(300);

            var recvStr2 = Encoding.Default.GetString(sendToPtlBuffer);
            Assert.Equal(recvStr2, cmd1 + cmd2);

            // para o loop do device
            bgWorker.CancelAsync();
        }

        private Otm.Plugins.Devices.RaiaMrcPTLv1.RaiaMrcPTLv1 CreateDevice(out BackgroundWorker backgroundWorker)
        {
            // Preparando ocenario para o teste
            var dvConfig = new DeviceConfig
            {
                Name = "ptl01",
                Driver = "ptl",
                Config = "ip=127.0.0.1;port=4660",
                Tags = (new DeviceTagConfig[]
                {
                    new DeviceTagConfig
                    {
                        Name = "cmd_count",
                        TypeCode = TypeCode.String,
                        Mode = Modes.ToOTM, // cmd rcvd from PTL
                        Address = "cmd_count"
                    },
                    new DeviceTagConfig
                    {
                        Name = "cmd_rcvd",
                        TypeCode = TypeCode.String,
                        Mode = Modes.ToOTM, // cmd rcvd from PTL
                        Address = "cmd_rcvd"
                    },
                    new DeviceTagConfig
                    {
                        Name = "cmd_send",
                        TypeCode = TypeCode.String,
                        Mode = Modes.FromOTM, // cmd send to PTL
                        Address = "cmd_send"
                    }
                }).ToList()
            };

            // Mock do TclClient
            var clientMock = new Mock<ITcpClientAdapter>();
            clientMock.Setup(x => x.Connected).Returns(() => true);
            clientMock.Setup(x => x.GetData())
                .Returns(() =>
                {
                    var tmp = recvFromPtlBuffer;
                    recvFromPtlBuffer = new byte[0];
                    return tmp;
                });

            clientMock.Setup(x => x.SendData(It.IsAny<byte[]>()))
                .Callback<byte[]>((x) =>
                {
                    var z = new byte[sendToPtlBuffer.Length + x.Length];
                    sendToPtlBuffer.CopyTo(z, 0);
                    x.CopyTo(z, sendToPtlBuffer.Length);

                    sendToPtlBuffer = z;
                });

            var loggerMock = new Mock<ILogger>();

            var devPtl = new Otm.Plugins.Devices.RaiaMrcPTLv1.RaiaMrcPTLv1();
            devPtl.Init(dvConfig, clientMock.Object, loggerMock.Object);
            var bgWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += (object o, DoWorkEventArgs args) =>
            {
                devPtl.Start(bgWorker);
            };
            backgroundWorker = bgWorker;

            return devPtl;
        }
    }
}
