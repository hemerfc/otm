using System;
using System.Linq;
using Xunit;
using Moq;
using Otm.Server.Device.Ptl;
using Otm.Shared.ContextConfig;
using NLog;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Threading;
using Otm.Server.Device.Palantir;

namespace Otm.Test.Device
{
    public class PalantirDeviceTests
    {
        private const char STX = (char)2;
        private const char ETX = (char)3;
        byte[] recvBuffer = new byte[0];
        byte[] sendBuffer = new byte[0];

        /*
            MENSAGEM: K01
            EXEMPLO: \x02K01,2019-01-21T13:44:23.455\x03
        */
        [Theory]
        // scanner read
        /*[InlineData("`GSTXLC|001|aaaETX`GSTXLC|002|bbbETX`GSTXLC|003|ccc|ETX",
            new string[] { "ptl01|LC|001|aaa", "ptl01|LC|002|bbb", "ptl01|LC|003|ccc" })]*/

        [InlineData("\x02K01,2019-01-21T13:44:23.455\x03",
            new string[] { "K01,2019-01-21T13:44:23.455" })]
        [InlineData("\x02K01,2019-0\x02K01,2019-01-21T13:44:23.455\x03",
            new string[] { "K01,2019-01-21T13:44:23.455" })]
        [InlineData("\x02K01,2019-01-21T13:44:23.455\x03\x02K02,2019-01-21T13:44:23.455\x02",
            new string[] { "K01,2019-01-21T13:44:23.455", "K02,2019-01-21T13:44:23.455" })]

        public void Receive_Ptl_Command(string recv, string[] result)
        {
            var waitEvent = new ManualResetEvent(false);

            recvBuffer = ASCIIEncoding.ASCII.GetBytes(recv);
            sendBuffer = new byte[0];

            var devPtl = CreateDevice(out BackgroundWorker bgWorker);

            // contador de 
            var commandsRcvd = 0;
            var falhaNoTeste = false;
            // prepara a funcao que dispara o trigger da trasação
            devPtl.OnTagChangeAdd("cmd_rcvd", (str, value) =>
            {
                // quando receber um commando do Ptl dispararia a transação,
                // mas para o teste apenas confirma o valor recebido
                if (result[commandsRcvd] != (string)value)
                    falhaNoTeste = true;

                commandsRcvd++;

                if (result.Length == commandsRcvd)
                    waitEvent.Set();
            });

            // inicia o loop do device
            bgWorker.RunWorkerAsync();

            waitEvent.WaitOne(500);

            Assert.False(falhaNoTeste); // se tem alguma mensagem diferente do esperado

            Assert.Equal(result.Length, commandsRcvd); // tem que receber o numero de comandos esperados 

            // para o loop do device
            bgWorker.CancelAsync();
        }

        [Fact]
        public void Send_Ptl_Command()
        {
            /*LOCATION|displayValue|masterMessage*/
            var cmd1 = $"001:002|{(byte)E_DisplayColor.Verde}|00000000001|{(int)E_PTLMasterMessage.None};001:003|1||{(int)E_PTLMasterMessage.ItemOk}";
            var cmd2 = $"001:005|{(byte)E_DisplayColor.Laranja}|00000000002|{(int)E_PTLMasterMessage.None};001:003|1||{(int)E_PTLMasterMessage.ToteOk}";
            
            recvBuffer = new byte[0];
            sendBuffer = new byte[0];

            var devPtl = CreateDevice(out BackgroundWorker bgWorker);

            // inicia o loop do device
            bgWorker.RunWorkerAsync();
            Thread.Sleep(50);

            devPtl.SetTagValue("cmd_send", cmd1);

            devPtl.SetTagValue("cmd_send", cmd2);
            Thread.Sleep(100);

            var recvStr2 = Encoding.Default.GetString(sendBuffer);
            Assert.Equal(recvStr2, cmd1 + cmd2);

            // para o loop do device
            bgWorker.CancelAsync();
        }

        private PtlDevice CreateDevice(out BackgroundWorker backgroundWorker)
        {
            // Preparando ocenario para o teste
            var dvConfig = new DeviceConfig
            {
                Name = "plc01",
                Driver = "pl",
                Config = "ip=127.0.0.1;port=3020",
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
                    var tmp = recvBuffer;
                    recvBuffer = new byte[0];
                    return tmp;
                });

            clientMock.Setup(x => x.SendData(It.IsAny<byte[]>()))
                .Callback<byte[]>((x) =>
                {
                    var z = new byte[sendBuffer.Length + x.Length];
                    sendBuffer.CopyTo(z, 0);
                    x.CopyTo(z, sendBuffer.Length);

                    sendBuffer = z;
                });

            var loggerMock = new Mock<ILogger>();

            var devPtl = new PalantirDevice();
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

