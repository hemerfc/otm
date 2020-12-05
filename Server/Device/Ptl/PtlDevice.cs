using System;
using System.Linq;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Otm.Shared.ContextConfig;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Diagnostics;

namespace Otm.Server.Device.Ptl
{
    public class PtlDevice : IDevice
    {
        private byte[] STX_LC = new byte[] { 0x02, 0x02 };       // "\x02\x02";
        private byte[] ETX_LC = new byte[] { 0x03, 0x03 };       // "\x03\x03";
        private byte[] STX_AT = new byte[] { 0x0f, 0x00, 0x60 }; //"\x0F\x00\x60";

        public string Name { get { return Config.Name; } }

        public BackgroundWorker Worker { get; private set; }

        private readonly Dictionary<string, Action<string, object>> tagsAction;
        private readonly object lockSendDataQueue = new object();
        private Queue<byte[]> sendDataQueue;
        private ILogger Logger;
        private DeviceConfig Config;
        private ITcpClientAdapter client;
        private string ip;
        private int port;
        //readonly bool firstLoadRead;
        //readonly bool firstLoadWrite;
        private bool Connecting;
        private DateTime lastConnectionTry;
        private string cmd_rcvd = "";
        private int cmd_count = 0;
        private const int RECONNECT_DELAY = 3000;
        public bool Ready { get; private set; }
        public DateTime LastSend { get; private set; }

        byte[] receiveBuffer = new byte[0];

        private object tagsActionLock;

        public bool Enabled { get { return true; } }
        public bool Connected { get { return client?.Connected??false; } }

        public DateTime LastErrorTime { get { return DateTime.Now; } }

        public IReadOnlyDictionary<string, object> TagValues { get { return null; } }

        public PtlDevice()
        {
            this.tagsAction = new Dictionary<string, Action<string, object>>();
            this.sendDataQueue = new Queue<byte[]>();
            tagsActionLock = new object();
        }

        public void Init(DeviceConfig dvConfig, ITcpClientAdapter client, ILogger logger)
        {
            this.client = client;
            Init(dvConfig, logger);
        }

        public void Init(DeviceConfig dvConfig, ILogger logger)
        {
            this.Logger = logger;
            this.Config = dvConfig;
            this.client = new TcpClientAdapter();
            GetConfig(dvConfig);
        }

        private void GetConfig(DeviceConfig dvConfig)
        {
            var cparts = dvConfig.Config.Split(';');

            this.ip = (cparts.FirstOrDefault(x => x.Contains("ip=")) ?? "").Replace("ip=", "").Trim();
            var strRack = (cparts.FirstOrDefault(x => x.Contains("port=")) ?? "").Replace("port=", "").Trim();

            this.port = 0;
            int.TryParse(strRack, out this.port);
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
            tagsAction[tagName] -= triggerAction;
        }

        public bool ContainsTag(string tagName)
        {
            return Config.Tags.Any(x => x.Name == tagName);
        }

        public DeviceTagConfig GetTagConfig(string name)
        {
            return Config.Tags.FirstOrDefault(x => x.Name == name);
        }

        public object GetTagValue(string tagName)
        {
            if (tagName == "cmd_rcvd")
                return cmd_rcvd;
            else if (tagName == "cmd_count")
                return cmd_count;
            else if (tagName == "ptl_ctrl")
                return Config.Name;

            return null;
        }

        private readonly List<PtlBaseClass> ListaLigados = new List<PtlBaseClass>();

        public void SetTagValue(string tagName, object value)
        {
            if (tagName != "cmd_send")
                Logger.LogError($"Dev {Config.Name}: O tag name '{tagName}', não é valido!");
            else if (value is string @string)
            {
                if (string.IsNullOrWhiteSpace((string)value))
                    return;
                // recebeu do datapoint 
                // criar os comandos pro PTL

                var ListaPendentes = (from rawPendente in ((string)value).Split(';').ToList()
                                      let pententeInfos = rawPendente.Split('|').ToList()
                                      select new PtlBaseClass(id: Guid.Parse(pententeInfos[4]),
                                                                    location: pententeInfos[0],
                                                                    displayColor: (E_DisplayColor)byte.Parse(pententeInfos[1]),
                                                                    displayValue: pententeInfos[2],
                                                                    masterMessage: (E_PTLMasterMessage)int.Parse(pententeInfos[3]))
                                                                    ).ToList();

                //Monta a lista do que é novo                                
                var ListaAcender = ListaPendentes.Where(i => !ListaLigados.Select(x => x.Id).Contains(i.Id));
                //Monsta a lista do que foi removido ou se for mensagem de 30 segundos atras
                var ListaApagar = ListaLigados.Where(i => !ListaPendentes.Select(x => x.Id).Contains(i.Id));



                lock (lockSendDataQueue)
                {
                    foreach (var itemApagar in ListaApagar.ToList())
                    {
                        //var buffer = Encoding.UTF8.GetBytes($"-{itemApagar}");
                        var buffer = new List<byte>();
                        byte displayId = itemApagar.GetDisplayId();

                        // set color { 0x00 -vermelho, 0x01 - verde, 0x02 - laranja, 0x03 - led off}
                        buffer.AddRange(new byte[] { 0x0A, 0x00, 0x60, 0x00, 0x00, 0x00, 0x1f, displayId, 0x00, (byte)E_DisplayColor.Off });
                        // limpa o display
                        buffer.AddRange(new byte[] { 0x08, 0x00, 0x60, 0x00, 0x00, 0x00, 0x01, displayId });

                        sendDataQueue.Enqueue(buffer.ToArray());
                        ListaLigados.Remove(itemApagar);
                    }

                    foreach (var itemAcender in ListaAcender.ToList())
                    {
                        /*
                        //Se for mensagem mestre, troca os valores para exebição
                        if (itemAcender.IsMasterMessage)
                        {
                            var (message, color) = itemAcender.MasterMessage.GetMessageAndColor();
                            itemAcender.SetColor(color);
                            itemAcender.SetDisplayValue(message);
                        }
                        */
                        byte displayId = itemAcender.GetDisplayId();
                        var displayCode = itemAcender.GetDisplayValueAsByteArray();

                        //9 Adicionando o pre + pos
                        byte msgLength = (byte)(displayCode.Length + 9);

                        var buf = new List<byte>();

                        // set color { 0x00 -vermelho, 0x01 - verde, 0x02 - laranja, 0x03 - led off}
                        buf.AddRange(new byte[] { 0x0A, 0x00, 0x60, 0x00, 0x00, 0x00, 0x1f, displayId, 0x00, (byte)itemAcender.DisplayColor });
                        //msgBuf.AddRange(new byte[] { 0x0A, 0x00, 0x60, 0x00, 0x00, 0x00, 0x1f, displayId, 0x00, 0x01 });

                        // "\x11\x00\x60\x66\x00\x00\x00\x64\x11\x4c\x4f\x47\x49\x4e\x20\x4f\x4b" -> LOGIN OK -> MODELO 70C(MESTRE)

                        var buf2 = new List<byte>();

                        if (itemAcender.IsMasterMessage)
                        {
                            //msgLength = (byte)(msgLength + 1);
                            buf2.AddRange(new byte[] { msgLength, 0x00, 0x60, 0x66, 0x00, 0x00, 0x00, displayId, 0x11 });
                            buf2.AddRange(displayCode);

                        }
                        else
                        {
                            buf2.AddRange(new byte[] { msgLength, 0x00, 0x60, 0x00, 0x00, 0x00, 0x00, displayId });
                            buf2.AddRange(displayCode);
                            buf2.Add(0x01);

                            if (itemAcender.IsBlinking)
                            {
                                buf2.AddRange(new byte[] { msgLength, 0x00, 0x60, 0x00, 0x00, 0x00, 0x11, displayId });
                                buf2.AddRange(displayCode);
                                buf2.Add(0x01);
                            }

                        }

                        buf.AddRange(buf2);
                        sendDataQueue.Enqueue(buf.ToArray());

                        ListaLigados.Add(itemAcender);
                    }


                }
            }
        }


        public void Stop()
        {
        }

        public void Start(BackgroundWorker worker)
        {
            // backgroud worker
            Worker = worker;

            while (true)
            {
                try
                {
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
                        ListaLigados.Clear();

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
                    waitEvent.WaitOne(150);

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
                    Logger.LogError($"Dev {Config.Name}: Update Loop Error {ex}");
                }
            }
        }

        private bool ReceiveData()
        {
            var received = false;

            var recv = client.GetData();
            if (recv != null && recv.Length > 0)
            {
                Logger.LogInformation($"ReceiveData(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Received: '{recv}'.\tString: '{ASCIIEncoding.ASCII.GetString(recv)}'\t ByteArray: '{string.Join(", ", recv)}'");
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
                var stxAtPos = SearchBytes(strRcvd, STX_AT);

                // se tem LC e ( nao tem AT ou tem mas esta depois do LC)
                // processa o LC
                if (stxLcPos >= 0 && (stxAtPos < 0 || (stxAtPos > stxLcPos)))
                {
                    // sem etx com stx
                    if (etxLcPos < 0 && stxLcPos > 0)
                        receiveBuffer = receiveBuffer[(stxLcPos + STX_LC.Length)..];
                    if (etxLcPos < stxLcPos)
                        receiveBuffer = receiveBuffer[(stxLcPos + STX_LC.Length)..];
                    else if (stxLcPos < etxLcPos)
                    {
                        receiveBuffer = receiveBuffer[(etxLcPos + ETX_LC.Length)..];

                        var cmdLC = Encoding.ASCII.GetString(strRcvd[(stxLcPos + STX_LC.Length)..etxLcPos]);
                        // LC|001|aaa => ptl01|LC|001|aaa
                        var cmdParts = cmdLC.Split('|');

                        var cmdType = cmdParts[0];
                        var cmdDevice = cmdParts[1];
                        var cmdValue = cmdParts[2];

                        var sendCMD = $"{Config.Name}|{cmdType}|{cmdDevice}|{cmdValue}";

                        cmd_rcvd = sendCMD;
                        cmd_count++;
                        received = true;

                        if (tagsAction.ContainsKey("cmd_rcvd"))
                        {
                            tagsAction["cmd_rcvd"]("cmd_rcvd", cmd_rcvd);
                        }
                        else if (tagsAction.ContainsKey("cmd_count"))
                        {
                            tagsAction["cmd_count"]("cmd_count", cmd_count);
                        }
                    }
                }
                else //if (stxLcPos >= 0 && (stxAtPos == -1 || (stxAtPos > stxLcPos)))
                {
                    /*if (etxLcPos >= 0)
                    {
                        receiveBuffer = receiveBuffer[(etxLcPos + ETX_LC.Length)..];
                    }
                    else*/ 
                    if (stxAtPos >= 0)
                    {
                        // processa o AT
                        var len = 15; // \x0F\x00\x60

                        var cmdAT = strRcvd[stxAtPos..(stxAtPos + len)];

                        // remove o commando do buffer
                        receiveBuffer = receiveBuffer[(stxAtPos + len)..];

                        var subCmd = cmdAT[6];
                        var subNode = cmdAT[7];
                        var cmdValue = Encoding.ASCII.GetString(cmdAT.Skip(8).Take(6).ToArray());

                        Logger.LogInformation($"ReceiveData(): Device: '{Config.Name}'. CmdAT: '{cmdAT}' subCmd:{subCmd} subNode:{subNode} cmdValue:{cmdValue}");

                        if (subCmd == 252)
                        {
                            Logger.LogInformation($"ReceiveData(): Device: '{Config.Name}'. subCmd: 252 IGNORADO");
                        }
                        else { 
                            var sendCMD = $"{Config.Name}|AT|{subNode:000}|{cmdValue}";
                            // ptl01|AT|001|000001
                            cmd_rcvd = sendCMD;
                            cmd_count++;
                            received = true;

                            if (tagsAction.ContainsKey("cmd_rcvd"))
                            {
                                lock (tagsActionLock)
                                {
                                    tagsAction["cmd_rcvd"]("cmd_rcvd", cmd_rcvd);
                                }
                            }
                            else if (tagsAction.ContainsKey("cmd_count"))
                            {
                                lock (tagsActionLock)
                                {
                                    tagsAction["cmd_count"]("cmd_count", cmd_count);
                                }
                            }
                        }
                    }
                }
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

                    Logger.LogDebug($"Dev {Config.Name}: Enviado {obj.Length} bytes em {st.ElapsedMilliseconds} ms.");


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
                client.Connect(ip, port);

                if (client.Connected)
                {
                    Logger.LogDebug($"Dev {Config.Name}: Connected.");
                }
                else
                {
                    Logger.LogError($"Dev {Config.Name}: Connection error.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Dev {Config.Name}: Connection error.");
            }
        }
    }
}
