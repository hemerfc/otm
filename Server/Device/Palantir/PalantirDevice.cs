using NLog;
using Otm.Server.Device.Palantir.Models;
using Otm.Server.Device.Ptl;
using Otm.Shared.ContextConfig;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Otm.Server.Device.Palantir
{
    public class PalantirDevice : IDevice
    {

        public PalantirDevice()
        {
            tagValues = new ConcurrentDictionary<string, object>();
            tagsAction = new ConcurrentDictionary<string, Action<string, object>>();
            this.sendDataQueue = new Queue<byte[]>();

        }

        private ILogger Logger;
        private DeviceConfig Config;
        private string IPServer;
        private int PortServer;
        private ITcpClientAdapter client;

        private byte[] STX_LC = new byte[] { MessageConstants.STX };
        private byte[] ETX_LC = new byte[] { MessageConstants.ETX };
        byte[] receiveBuffer = new byte[0];

        #region CustomPallantirDeviceParams

        public DateTime LastSendKeepAliveTry { get; set; } = DateTime.MinValue;
        public DateTime LastSendKeepAliveSuccess { get; set; } = DateTime.Now;
        private bool ReconnectRequest = false;

        #endregion CustomPallantirDeviceParams

        #region FromPTLParams

        private string cmd_rcvd = "";
        private int cmd_count = 0;
        private Queue<byte[]> sendDataQueue;
        private readonly object lockSendDataQueue = new object();
        public DateTime LastSend { get; private set; }
        private bool Connecting;
        private DateTime lastConnectionTry;
        //private string ip;
        //private int port;
        private const int RECONNECT_DELAY = 3000;

        #endregion FromPTLParams

        private readonly ConcurrentDictionary<string, object> tagValues;
        private readonly ConcurrentDictionary<string, Action<string, object>> tagsAction;

        public object tagsActionLock = new object();

        public bool Ready { get; private set; }

        public BackgroundWorker Worker { get; private set; }

        public int LicenseRemainingHours { get; set; }
        public DateTime? LastUpdateDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string Name { get { return Config.Name; } }

        public bool Enabled { get { return true; } }

        public bool Connected = false;

        public DateTime LastErrorTime { get { return DateTime.Now; } }

        public IReadOnlyDictionary<string, object> TagValues => throw new NotImplementedException();

        bool IDeviceStatus.Connected => Connected;

        public bool ContainsTag(string tagName)
        {
            return Config.Tags.Any(x => x.Name == tagName);
        }

        public void GetLicenseRemainingHours()
        {
            LicenseRemainingHours = int.MaxValue;
        }

        public DeviceTagConfig GetTagConfig(string name)
        {
            return Config.Tags.FirstOrDefault(x => x.Name == name);
        }

        public object GetTagValue(string tagName)
        {
            return tagValues[tagName];
        }

        public void Init(DeviceConfig dvConfig, ITcpClientAdapter client, ILogger logger)
        {
            this.client = client;
            this.Logger = logger;
            this.Config = dvConfig;
            GetConfig(dvConfig);
        }

        public void Init(DeviceConfig dvConfig, ILogger logger)
        {
            this.Logger = logger;
            this.Config = dvConfig;
            this.client = new TcpClientAdapter();
            GetConfig(dvConfig);
        }

        public void OnTagChangeAdd(string tagName, Action<string, object> triggerAction)
        {
            var tagConfig = GetTagConfig(tagName);

            // can't use a output tag as trigger, output put tags are writed to PLC
            if (tagConfig.Mode == Modes.FromOTM) // from OTM to device
            {
                throw new Exception("Error can't put a trigger on a input tag");
            }
            if (!tagsAction.ContainsKey(tagName))
                tagsAction[tagName] = triggerAction;
            else
                tagsAction[tagName] += triggerAction;
        }

        public void OnTagChangeRemove(string tagName, Action<string, object> triggerAction)
        {
            throw new NotImplementedException();
        }

        public void SetTagValue(string tagName, object value)
        {
            tagValues[tagName] = value;
            Logger.Debug($"PalantirDevice|SetTagValue|TagName: '{tagName}'|TagValues: '{value}'");
        }

        /*
        public void GetData()
        {
            try
            {
                if (client.Available > 0)
                {
                    var buffer = new byte[client.Available];
                    if (buffer.Length > 0)
                    {

                        client.Client.Receive(buffer);
                        Received(buffer);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"PalantirDevice|GetData|error new message:'{ex.Message}'");
            }
        }*/

        private bool ReceiveData()
        {
            var received = false;

            var recv = client.GetData();
            if (recv != null && recv.Length > 0)
            {
                Logger.Info($"PalantirDevice|ReceiveData(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Received: '{recv}'.\tString: '{ASCIIEncoding.ASCII.GetString(recv)}'\t ByteArray: '{string.Join(", ", recv)}'");
                var tempBuffer = new byte[receiveBuffer.Length + recv.Length];
                receiveBuffer.CopyTo(tempBuffer, 0);
                recv.CopyTo(tempBuffer, receiveBuffer.Length);
                receiveBuffer = tempBuffer;
            }

            if (receiveBuffer.Length > 0)
            {
                //var strRcvd = Encoding.ASCII.GetString(receiveBuffer);
                var strRcvd = receiveBuffer;

                /// TODO: falta processar os bytes recebidos
                var stxLcPos = SearchBytes(strRcvd, STX_LC);
                var etxLcPos = SearchBytes(strRcvd, ETX_LC);

                var posicoesRelevantesEncontradas = new List<int>() { stxLcPos }
                                                            .Where(x => x >= 0)
                                                            .OrderBy(x => x)
                                                            .ToList();

                //Se encontrou algo relevante processa, senão zera...
                if (posicoesRelevantesEncontradas.Count > 0)
                {
                    received = true;

                    var primeiraPosRelevante = posicoesRelevantesEncontradas.First();
                    //Filtra o array para remover o lixo do inicio
                    //receiveBuffer = receiveBuffer[primeiraPosRelevante..];

                    //Se for uma leitura, verifica se ja tem o STX
                    if (primeiraPosRelevante == stxLcPos)
                    {
                        //Aguarda encontrar o ETX
                        if (stxLcPos < etxLcPos)
                        {


                            var cmdLC = Encoding.ASCII.GetString(strRcvd[(stxLcPos + STX_LC.Length)..etxLcPos]);

                            ProcessCompleteMessage(cmdLC);
                                                        
                            //Descartando a leitura do buffer pois ja foi processada
                            //received = true;
                            receiveBuffer = receiveBuffer[(etxLcPos + STX_LC.Length)..];
                        }
                    }
                }
                else
                    receiveBuffer = Array.Empty<byte>();


            }

            return received;
        }



        private static int SearchBytes(byte[] haystack, byte[] needle)
        {
            var len = needle.Length;
            var limit = haystack.Length - len;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    if (needle[k] != haystack[i + k]) break;
                }
                if (k == len) return i;
            }
            return -1;
        }

        private bool SendData()
        {
            var sent = false;

            if (sendDataQueue.Count > 0)
                lock (lockSendDataQueue)
                {
                    var st = new Stopwatch();
                    st.Start();

                    var totalLength = 0;
                    foreach (var it in sendDataQueue)
                    {
                        totalLength += it.Length;
                    }

                    var obj = new byte[totalLength];
                    var pos = 0;
                    while (sendDataQueue.Count > 0)
                    {
                        var it = sendDataQueue.Dequeue();
                        Array.Copy(it, 0, obj, pos, it.Length);
                        pos += it.Length;
                    }

                    client.SendData(obj);

                    st.Stop();

                    Logger.Debug($"Dev {Config.Name}: Enviado {obj.Length} bytes em {st.ElapsedMilliseconds} ms.");


                    /*
                    while (sendDataQueue.Count > 0)
                    {
                        var obj = sendDataQueue.Dequeue();
                        client.SendData(obj);
                        sent = true;
                    }
                    */
                    this.LastSend = DateTime.Now;
                }
            else
            {
                if (LastSend.AddSeconds(3) < DateTime.Now)
                {
                    var getFwCmd = new byte[] { 0x07, 0x00, 0x60, 0x00, 0x00, 0x00, 0x09 };
                    client.SendData(getFwCmd);
                    this.LastSend = DateTime.Now;
                }
            }

            return sent;
        }


        private void Connect()
        {
            try
            {
                client.Connect(IPServer, PortServer);

                if (client.Connected)
                {
                    Logger.Debug($"Dev {Config.Name}: Connected.");
                }
                else
                {
                    Logger.Error($"Dev {Config.Name}: Connection error.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Dev {Config.Name}: Connection error.");
            }
        }


        public void Start(BackgroundWorker worker)
        {
            /*
            Int32 port = Int32.Parse(PortServer);
            Connect(IPServer, port);
            while (true)
            {
                try
                {
                    SendKeepAlive();

                    if (client.Connected && !ReconnectRequest)
                    {
                        //PrepareSendData();
                        GetData();
                    }
                    else
                    {
                        Connect(IPServer, port);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"PalantirDevice|start sendData|error: '{e.Message}'");
                }
            }*/

            // backgroud worker
            Worker = worker;

            GetLicenseRemainingHours();

            while (LicenseRemainingHours > 0)
            {
                try
                {
                    SendKeepAlive();

                    if (client.Connected)
                    {
                        bool received, sent;

                        do
                        {
                            received = ReceiveData();
                            sent = SendData();
                        } while (received || sent);

                        Ready = true;
                    }
                    else
                    {
                        Ready = false;
                        //ListaLigados.Clear();

                        if (!Connecting)
                        {
                            // se ja tiver passado o delay, tenta reconectar
                            if (lastConnectionTry.AddMilliseconds(RECONNECT_DELAY) < DateTime.Now)
                            {
                                lastConnectionTry = DateTime.Now;
                                Connecting = true;
                                //Verifica se consegue conectar
                                Connect();
                                Connecting = false;
                            }
                        }
                    }
                    // wait 50ms
                    /// TODO: wait time must be equals the minimum update rate of tags
                    var waitEvent = new ManualResetEvent(false);
                    waitEvent.WaitOne(50);

                    if (Worker.CancellationPending)
                    {
                        Ready = false;
                        Stop();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Ready = false;
                    Logger.Error($"Dev {Config.Name}: Update Loop Error {ex}");
                }
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        private void GetConfig(DeviceConfig dvConfig)
        {
            GetDeviceParameter(dvConfig);
            GetDeviceTags(dvConfig);
        }

        private void GetDeviceParameter(DeviceConfig dvConfig)
        {
            try
            {
                var cparts = dvConfig.Config.Split(';');

                IPServer = (cparts.FirstOrDefault(x => x.Contains("IPServer=")) ?? "").Replace("IPServer=", "").Trim();

                var strPortServer = (cparts.FirstOrDefault(x => x.Contains("PortServer=")) ?? "").Replace("PortServer=", "").Trim();
                if (!int.TryParse(strPortServer, out PortServer))
                    throw new Exception($"Não foi possível converter a porta '{strPortServer}' em int ");
            }
            catch (Exception ex)
            {
                Logger.Error($"FileDevice ({Config.Name})|GetDeviceParameter|Error: {ex}");
                throw;
            }
        }
        private void GetDeviceTags(DeviceConfig dvConfig)
        {
            try
            {
                //routingKey = dvConfig.Tags.FirstOrDefault(x => x.Name == nameof(routingKey)).Name ?? "*";
            }
            catch (Exception ex)
            {
                Logger.Error($"FileDevice ({Config.Name})|GetDeviceTags|Error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// A mensagem K01 deve ser enviada pelo OTM ao PLC a cada 5 segundos
        /// o PLC por sua vez responde a mensagem K02
        /// se alguns dos lados fica sem receber a menagem por 10 segundos este deve finalizar esta conexão e iniciar uma nova conexão. 
        /// Este processo garante que a conexão esta saldável e que ambos as partes estão aptas a responder as requisições. 
        /// </summary>
        private void SendKeepAlive()
        {
            var execTime = DateTime.Now;
            if (LastSendKeepAliveTry.AddMilliseconds(MessageConstants.K01_INTERVAL_MS) <= execTime)
            {
                Logger.Debug($"PalantirDevice|SendKeepAlive|Incluindo uma K01 na fila de envio...");

                var keepAliveMessage = new MessageKeepAlive(MessageCodesKeepAlive.ClientSend);

                sendDataQueue.Enqueue(keepAliveMessage.GetCompleteMessage());

                /*
                //Envia a mensagem e verifica se foi enviado corretamente
                if (!SendData(keepAliveMessage.GetCompleteMessage()))
                {
                    Logger.Debug($"PalantirDevice|SendKeepAlive|Ocorreu um erro ao enviar a K01!");

                    //Verifica se passou mais o tempo de timout da ultima conexão com sucesso para reconexão
                    if (LastSendKeepAliveSuccess.AddMilliseconds(MessageConstants.K01_TIMEOUT_MS) <= execTime)
                    {
                        Logger.Debug($"PalantirDevice|SendKeepAlive|Executar desconexão por timeout!");
                        ReconnectRequest = true;
                    }
                    else
                    {
                        Logger.Debug($"PalantirDevice|SendKeepAlive|Não conectado, tentando até terminar o timeout!");
                    }

                }
                else
                {
                    Logger.Debug($"PalantirDevice|SendKeepAlive|K01 Enviada com sucesso!");
                    LastSendKeepAliveSuccess = execTime;
                }


                */
                LastSendKeepAliveTry = execTime;
                Logger.Debug($"PalantirDevice|SendKeepAlive|K01 incluída na fila de envio!");
            }
        }

        /// <summary>
        /// Função para separar a string por virgula, a menos que ele esteja dentro de aspas duplas.
        /// Créditos para base de código: ChatGPT
        /// </summary>
        /// <param name="input">Mensagem completa para ser dividida</param>
        /// <returns></returns>
        static string[] ParseMessage(string input)
        {
            string[] items = new string[input.Length / 2];
            int itemCount = 0;
            string currentItem = "";

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '"')
                {
                    i++;
                    while (i < input.Length && input[i] != '"')
                    {
                        currentItem += input[i++];
                    }
                }
                else if (input[i] == ',')
                {
                    items[itemCount++] = currentItem;
                    currentItem = "";
                }
                else
                {
                    currentItem += input[i];
                }
            }

            itemCount++;
            items[itemCount - 1] = currentItem;
            string[] result = new string[itemCount];
            Array.Copy(items, result, itemCount);
            return result;
        }

        /// <summary>
        /// Recebe uma mensagem completa e a processa de acordo com seus parametros.
        /// </summary>
        /// <param name="rawMessage">Mensagem completa para processamento</param>
        private void ProcessCompleteMessage(string rawMessage)
        {
            Logger.Debug($"PalantirDevice|ProcessCompleteMessage|rawMessage: {rawMessage}");

            //Obtendo o tipo da mensagem
            string[] messageItems = ParseMessage(rawMessage);

            if (messageItems.Length <= 0)
            {
                Logger.Error($"PalantirDevice|ProcessCompleteMessage|Quantidade insuficiente de parametros extraídos da mensagem");
            }
            else
            {
                switch (messageItems[0])
                {
                    case "K02":
                        {
                            Logger.Debug($"PalantirDevice|ProcessCompleteMessage|Mensagem KeepAlive - K02");
                            var messageDatetime = messageItems[1];
                            Logger.Debug($"PalantirDevice|ProcessCompleteMessage|Mensagem KeepAlive - K02 | messageDatetime: {messageDatetime}");
                        }
                        break;
                    case "R01":
                        {
                            Logger.Debug($"PalantirDevice|ProcessCompleteMessage|Mensagem KeepAlive - K02");
                            var messageDatetime = messageItems[1];
                            Logger.Debug($"PalantirDevice|ProcessCompleteMessage|Mensagem KeepAlive - K02 | messageDatetime: {messageDatetime}");
                        }
                        break;
                    default:
                        {
                            Logger.Error($"PalantirDevice|ProcessCompleteMessage|Tipo de mensagem '{messageItems[0]}' não conhecido");
                        }
                        break;
                }
            }
        }

    }
}
