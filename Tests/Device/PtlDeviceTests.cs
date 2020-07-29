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

        /*
            CARTAO
            Definicao: ´G|TIPO|LEITOR|CODIGO_CARTAO
            Exemplo: ´G|LC|1|123456|
            LEITURA
            Definicao ´G|TIPO|LEITOR|CODIGO_DOCTO
            Exemplo ´G|LC|1|0000001685100B|
            ATENDIMENTO
            Definicao ´G|TIPO|IDENTIFICADOR_POSICAO|VALOR
            Exemplo ´G|AT|F01.L01.C04|5|
        */
        [Theory]
        // scanner read
        /*[InlineData("`GSTXLC|001|aaaETX`GSTXLC|002|bbbETX`GSTXLC|003|ccc|ETX",
            new string[] { "ptl01|LC|001|aaa", "ptl01|LC|002|bbb", "ptl01|LC|003|ccc" })]
        [InlineData("\x0F\x00\x60\x00\x00\x00\x06\x01\x30\x30\x30\x30\x30\x31\x00" + // Cmd=6 Node=1 Value=1 Dot=0
                    "\x0F\x00\x60\x00\x00\x00\x06\x01\x30\x30\x30\x30\x30\x32\x00" + // Cmd=6 Node=1 Value=1 Dot=0
                    "\x0F\x00\x60\x00\x00\x00\x06\x01\x30\x30\x30\x30\x30\x33\x00",  // Cmd=6 Node=1 Value=1 Dot=0
            new string[] { "ptl01|AT|001|000001", "ptl01|AT|001|000002", "ptl01|AT|001|000003" })]
*/

        [InlineData("\x0F\x00\x60\x00\x00\x00\x06\x01\x30\x30\x30\x30\x30\x31\x00" + // Cmd=6 Node=1 Value=1 Dot=0
                    "lixoaqui!" +
                    "`GSTXLC|001|aaaETX",
            //"\x0F\x00\x60\x00\x00\x00\x06\x01\x30\x30\x30\x30\x30\x32\x00" + // Cmd=6 Node=1 Value=1 Dot=0
            //"`GSTXLC|002|bbbETX`GSTXLC|003|ccc|ETX" +
            //"lixoaquidenovo!" +
            //"\x0F\x00\x60\x00\x00\x00\x06\x01\x30\x30\x30\x30\x30\x33\x00",  // Cmd=6 Node=1 Value=1 Dot=0
            new string[] { "ptl01|AT|001|000001", "ptl01|LC|001|aaa" /*, "ptl01|AT|001|000002",
                           "ptl01|LC|002|bbb", "ptl01|LC|003|ccc", "ptl01|AT|001|000003"*/ })]
        public void Receive_Ptl_Command(string recv, string[] result)
        {
            var waitEvent = new ManualResetEvent(false);

            recvFromPtlBuffer = Encoding.ASCII.GetBytes(recv);
            sendToPtlBuffer = new byte[0];

            var devPtl = CreateDevice(out BackgroundWorker bgWorker);

            // contador de 
            var commandsRcvd = 0;
            // prepara a funcao que dispara o trigger da trasação
            devPtl.OnTagChangeAdd("cmd_rcvd", (str, value) =>
            {
                // quando receber um commando do Ptl dispararia a transação,
                // mas para o teste apenas confirma o valor recebido
                Assert.Equal(result[commandsRcvd], (string)value);

                commandsRcvd++;

                if (result.Length == commandsRcvd)
                    waitEvent.Set();
            });

            // inicia o loop do device
            bgWorker.RunWorkerAsync();

            waitEvent.WaitOne(500);

            Assert.Equal(result.Length, commandsRcvd); // tem que receber dois commandos

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
            Thread.Sleep(50);

            devPtl.SetTagValue("cmd_send", cmd1);

            devPtl.SetTagValue("cmd_send", cmd2);
            Thread.Sleep(100);

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

