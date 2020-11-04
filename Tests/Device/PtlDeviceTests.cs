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

        [InlineData(new byte[] {
                /* LC */2, 2, 76, 67, 124, 48, 48, 50, 124, 105, 48, 48, 48, 48, 49, 50, 52, 54, 49, 51, 49, 48, 48, 48, 48, 51, 3, 3,
                /* Lixo */36, 0, 96, 0, 0, 0, 6, 40,
                /* Abrindo read gate */0x14,   0x00,    0x60,   0x00,    0x00,    0x00,    0x06,    0x27,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,	 //Atendimento 012345678901
                /* LC */2, 2, 76, 67, 124, 48, 48, 50, 124, 105, 48, 48, 48, 48, 49, 50, 52, 54, 49, 51, 49, 48, 48, 48, 48, 52, 3, 3,
                /* LC */2, 2, 76, 67, 124, 48, 48, 50, 124, 105, 48, 48, 48, 48, 49, 50, 52, 54, 49, 51, 49, 48, 48, 48, 48, 53, 3, 3
            },
            new string[] { "ptl01|LC|002|i0000124613100004" })]
        [InlineData(new byte[] {
                /* Lixo */36, 0, 96, 0, 0, 0, 6, 40,
                /* Abrindo read gate */0x14,   0x00,    0x60,   0x00,    0x00,    0x00,    0x06,    0x27,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,	 //Atendimento 012345678901
                /* Lixo */36, 0, 96, 0, 0, 0, 6, 40,
                /* LC */2, 2, 76, 67, 124, 48, 48, 50, 124, 105, 48, 48, 48, 48, 49, 50, 52, 54, 49, 51, 49, 48, 48, 48, 48, 51, 3, 3,
                /* AT */0x0f, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x32, 0x00, // AT
                /* Abrindo read gate */0x14,   0x00,    0x60,   0x00,    0x00,    0x00,    0x06,    0x27,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,	 //Atendimento 012345678901
                /* LC */2, 2, 76, 67, 124, 48, 48, 50, 124, 105, 48, 48, 48, 48, 49, 50, 52, 54, 49, 51, 49, 48, 48, 48, 48, 51, 3, 3,
                /* LC */2, 2, 76, 67, 124, 48, 48, 50, 124, 105, 48, 48, 48, 48, 49, 50, 52, 54, 49, 51, 49, 48, 48, 48, 48, 52, 3, 3,
            },
            new string[] { "ptl01|LC|002|i0000124613100003",
                           "ptl01|AT|017|     2",
                           "ptl01|LC|002|i0000124613100003" })]
        [InlineData(new byte[] {
                0x0f, 0x00, 0x60, 0x00, 0x00, 0x00, 0xfc, 0x16, 0x02, 0x13, 0x73, 0x08, 0x02, 0x00, 0x02, // STATUS Cmd=252
                0x0f, 0x00, 0x60, 0x00, 0x00, 0x00, 0xfc, 0x17, 0x02, 0x13, 0x73, 0x08, 0x02, 0x00, 0x02, // STATUS Cmd=252
                0x0c, 0x00, 0x60, 0x00, 0x00, 0x00, 0xfc, 0x27, 0x02, 0x10, 0x30, 0x08,                   // STATUS Cmd=252
                0x0f, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x32, 0x00  // Cmd=6 Node=17 Value=2 Dot=0
            },
            new string[] { "ptl01|AT|017|     2" })]
        [InlineData(new byte[]{
                0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00, // Cmd=6 Node=17 Value=1 Dot=0
                0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x12, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00  // Cmd=6 Node=18 Value=1 Dot=0 
            },
            new string[] { "ptl01|AT|017|     3", "ptl01|AT|018|     3" })]
        [InlineData(new byte[]{
                0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00, // Cmd=6 Node=17 Value=1 Dot=0
                0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20,                         // lixo
                0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20,                         // lixo
                0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x12, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00  // Cmd=6 Node=18 Value=1 Dot=0 
            },
            new string[] { "ptl01|AT|017|     3", "ptl01|AT|018|     3" })]
        [InlineData(new byte[]{
                0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00, // Cmd=6 Node=17 Value=1 Dot=0
                0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00, // Cmd=6 Node=17 Value=1 Dot=0
                0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20,                         // lixo
                0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20,                         // lixo
                0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x12, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00  // Cmd=6 Node=18 Value=1 Dot=0 
            },
            new string[] { "ptl01|AT|017|     3", "ptl01|AT|017|     3", "ptl01|AT|018|     3" })]
        [InlineData(new byte[] {
                /* Lixo */36, 0, 96, 0, 0, 0, 6, 40,
                /*Cmd=6 Node=17 Value=1 Dot=0*/0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00,  
                /* Abrindo read gate */0x14,   0x00,    0x60,   0x00,    0x00,    0x00,    0x06,    0x27,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,	 //Atendimento 012345678901
                /*Cmd=6 Node=17 Value=1 Dot=0*/0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00,  
                /* Lixo */36, 0, 96, 0, 0, 0, 6, 40,
                /* LC */2, 2, 76, 67, 124, 48, 48, 50, 124, 105, 48, 48, 48, 48, 49, 50, 52, 54, 49, 51, 49, 48, 48, 48, 48, 51, 3, 3,
                /* AT */0x0f, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x32, 0x00, 
                /* Abrindo read gate */0x14,   0x00,    0x60,   0x00,    0x00,    0x00,    0x06,    0x27,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,   0x5A,	 //Atendimento 012345678901
                /* LC */2, 2, 76, 67, 124, 48, 48, 50, 124, 105, 48, 48, 48, 48, 49, 50, 52, 54, 49, 51, 49, 48, 48, 48, 48, 51, 3, 3,
                /* LC */2, 2, 76, 67, 124, 48, 48, 50, 124, 105, 48, 48, 48, 48, 49, 50, 52, 54, 49, 51, 49, 48, 48, 48, 48, 52, 3, 3,
                /*Cmd=6 Node=17 Value=1 Dot=0*/0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00,  
                /*Cmd=6 Node=17 Value=1 Dot=0*/0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00, 
                /*lixo*/0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20,                          
                /*lixo*/0x60, 0x00, 0x00, 0x00, 0x06, 0x11, 0x20, 0x20, 0x20, 0x20, 0x20,                         
                /*Cmd=6 Node=18 Value=1 Dot=0  */0x0F, 0x00, 0x60, 0x00, 0x00, 0x00, 0x06, 0x12, 0x20, 0x20, 0x20, 0x20, 0x20, 0x33, 0x00   
            },
            new string[] {
                            "ptl01|AT|017|     3",
                            "ptl01|AT|017|     3",
                            "ptl01|LC|002|i0000124613100003",
                           "ptl01|AT|017|     2",
                           "ptl01|LC|002|i0000124613100003",
                            "ptl01|AT|017|     3", 
                            "ptl01|AT|017|     3", 
                            "ptl01|AT|018|     3"})]
        public void Receive_Ptl_Command(byte[] recv, string[] result)
        {
            var waitEvent = new ManualResetEvent(false);

            recvFromPtlBuffer = recv;
            sendToPtlBuffer = new byte[0];

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

            Assert.False(falhaNoTeste); // tem que receber dois commandos

            Assert.Equal(result.Length, commandsRcvd); // tem que receber dois commandos

            // para o loop do device
            bgWorker.CancelAsync();
        }

        [Fact]
        public void Send_Ptl_Command()
        {
            /*LOCATION|displayValue|masterMessage*/
            var cmd1 = $"001:002|{(byte)E_DisplayColor.Verde}|00000000001|{(int)E_PTLMasterMessage.None};001:003|1||{(int)E_PTLMasterMessage.ItemOk}";
            var cmd2 = $"001:005|{(byte)E_DisplayColor.Laranja}|00000000002|{(int)E_PTLMasterMessage.None};001:003|1||{(int)E_PTLMasterMessage.ToteOk}";

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
                Config = "ip=127.0.0.1;port=4660;MasterDevice=39",
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

