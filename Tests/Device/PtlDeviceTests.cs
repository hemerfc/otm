using System;
using System.Linq;
using Xunit;
using Moq;
using Otm.Server.Device.Ptl;
using Otm.Shared.ContextConfig;
using Microsoft.Extensions.Logging;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace Otm.Test.Device
{
    public class PtlDeviceTests
    {
        private const char STX = '\u0002';
        private const char ETX = '\u0003';
        byte[] recvFromPtlBuffer = new byte[0];
        byte[] sendToPtlBuffer = new byte[0];

        [Fact]
        public void Receive_Ptl_Command()
        {
            var cmd1 = "|LC01|LEITURA123|";
            var cmd2 = "|COMANDO|";
            recvFromPtlBuffer = Encoding.UTF8.GetBytes($"{STX}{cmd1}{ETX}{STX}{cmd2}{ETX}");
            sendToPtlBuffer = new byte[0];

            var devPtl = CreateDevice(out BackgroundWorker bgWorker);

            // contador de 
            var commandsRcvd = 0;
            // prepara a funcao que dispara o trigger da trasação
            devPtl.OnTagChangeAdd("cmd_rcvd", (str, value) =>
            {
                // quando receber um commando do Ptl dispararia a transação,
                // mas para o teste apenas confirma o valor recebido
                if (commandsRcvd == 0) // primeiro commando
                    Assert.Equal((string)value, cmd1);

                if (commandsRcvd == 1) // segundo commando
                    Assert.Equal((string)value, cmd2);

                commandsRcvd++;
            });

            // inicia o loop do device
            bgWorker.RunWorkerAsync();

            Thread.Sleep(500);

            Assert.Equal(2, commandsRcvd); // tem que receber dois commandos

            // para o loop do device
            bgWorker.CancelAsync();
        }

        [Fact]
        public void Send_Ptl_Command()
        {
            var cmd1 = "|CMD001|";
            var cmd2 = "|CMD002|";
            recvFromPtlBuffer = new byte[0];
            sendToPtlBuffer = new byte[0];

            var devPtl = CreateDevice(out BackgroundWorker bgWorker);

            // inicia o loop do device
            bgWorker.RunWorkerAsync();
            Thread.Sleep(500);

            devPtl.SetTagValue("cmd_send", cmd1);
            Thread.Sleep(500);

            var recvStr1 = Encoding.Default.GetString(sendToPtlBuffer);
            Assert.Equal(recvStr1, cmd1);

            devPtl.SetTagValue("cmd_send", cmd2);
            Thread.Sleep(500);

            var recvStr2 = Encoding.Default.GetString(sendToPtlBuffer);
            Assert.Equal(recvStr2, cmd1 + cmd2);

            // para o loop do device
            bgWorker.CancelAsync();
        }

        private PtlDevice CreateDevice(out BackgroundWorker backgroundWorker)
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

            var devPtl = new PtlDevice(dvConfig, clientMock.Object, loggerMock.Object);

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

