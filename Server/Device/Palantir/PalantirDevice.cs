using Nest;
using NLog;
using Otm.Server.Device.Palantir.Models;
using Otm.Shared.ContextConfig;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security;
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
        }

        private Logger Logger;
        private DeviceConfig Config;
        private string IPServer;
        private string PortServer;
        private TcpClient client;
        private bool ReconnectRequest = false;


        private byte[] STX_LC = new byte[] { MessageConstants.STX };
        private byte[] ETX_LC = new byte[] { MessageConstants.ETX };
        byte[] receiveBuffer = new byte[0];
        private bool readGateOpen;
        private bool hasReadGate;

        private readonly ConcurrentDictionary<string, object> tagValues;
        private readonly ConcurrentDictionary<string, Action<string, object>> tagsAction;

        public object tagsActionLock = new object();

        public bool Ready => throw new NotImplementedException();

        public BackgroundWorker Worker => throw new NotImplementedException();

        public int LicenseRemainingHours { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
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
            throw new NotImplementedException();
        }

        public DeviceTagConfig GetTagConfig(string name)
        {
            return Config.Tags.FirstOrDefault(x => x.Name == name);
        }

        public object GetTagValue(string tagName)
        {
            return tagValues[tagName];
        }

        public void Init(DeviceConfig dvConfig, Logger logger)
        {
            Logger = logger;
            Config = dvConfig;
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
        }

        public bool Received(byte[] recv)
        {
            var received = false;

            if (recv != null && recv.Length > 0)
            {
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
                            //Processa se o ReadGate estiver aberto e fecha-o em seguida
                            if (true)
                            {

                                var cmdLC = Encoding.ASCII.GetString(strRcvd[(stxLcPos + STX_LC.Length)..etxLcPos]);

                                var tagTriggers = new List<(Action<string, object> func, string tagName, object tagValue)>();


                                Logger.Debug($"PalantirDevice|Received|new message:'{cmdLC}'");

                                SetTagValue("data_message", cmdLC);

                                if (tagsAction.ContainsKey("data_message"))
                                {
                                    tagTriggers.Add(new(tagsAction["data_message"], "data_message", tagValues["data_message"]));

                                    ProcessCompleteMessage((string)tagValues["data_message"]);
                                }

                                foreach (var tt in tagTriggers)
                                {
                                    lock (tagsActionLock)
                                    {
                                        tt.func(tt.tagName, tt.tagValue);

                                    }
                                }


                            }
                            receiveBuffer = receiveBuffer[(etxLcPos + STX_LC.Length)..];
                        }
                    }
                }
                else
                    receiveBuffer = Array.Empty<byte>();
            }

            return received;
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



        public async void PrepareSendData()
        {
            try
            {
                //byte[] buffer = System.Text.Encoding.ASCII.GetBytes("true");
                byte[] buffer = Encoding.ASCII.GetBytes($"{0x02}{0x63}{0x03}");
                SendData(buffer);
            }
            catch (Exception e)
            {
                Logger.Error($"PalantirDevice|SendData|error: '{e.Message}");
            }

        }

        public bool SendData(byte[] message)
        {
            try
            {
                client.Client.Send(message);
                //Thread.Sleep(1000);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"PalantirDevice|SendData|error: '{e.Message}");
                return false;
            }

        }


        public DateTime LastSendKeepAliveTry { get; set; } = DateTime.MinValue;
        public DateTime LastSendKeepAliveSuccess { get; set; } = DateTime.Now;

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
                Logger.Debug($"PalantirDevice|SendKeepAlive|Inicio do envio da K01...");

                var keepAliveMessage = new MessageKeepAlive(MessageCodesKeepAlive.ClientSend);

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


                LastSendKeepAliveTry = execTime;
                Logger.Debug($"PalantirDevice|SendKeepAlive|Fim do envio da K01!");
            }
        }










        public void Connect(string ip, int port)
        {
            try
            {
                client = new TcpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                client.Connect(IPAddress.Parse(ip), port);
                if (client.Connected)
                {
                    Logger.Debug($"PalantirDevice|Connect|client Connected in Ip:'{IPServer}' and port:'{port}'");

                    ReconnectRequest = false;
                }
            }
            catch (Exception e)
            {
                Logger.Debug($"PalantirDevice|Connect|error: '{e.Message}'");
            }

        }


        public void Start(BackgroundWorker worker)
        {
            int port = int.Parse(PortServer);
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
                PortServer = (cparts.FirstOrDefault(x => x.Contains("PortServer=")) ?? "").Replace("PortServer=", "").Trim();
            }
            catch (Exception ex)
            {
                Logger.Error($"PalantirDevice| ({Config.Name})|GetDeviceParameter|Error: {ex}");
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
                Logger.Error($"PalantirDevice| ({Config.Name})|GetDeviceTags|Error: {ex}");
                throw;
            }
        }
    }
}
