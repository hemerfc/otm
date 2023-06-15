using NLog;
using Otm.Server.Device;
using Otm.Server.Device.Ptl;
using Otm.Server.Device.TcpServer;
using Otm.Server.ContextConfig;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Otm.Plugins.Devices.RaiaMrcPTLv1
{
    /// <summary>
    /// Device que envia mensagens (socket) para o WMS Manhatan da Raia
    /// sempre que um picking do PTL for terminado
    /// </summary>
    public class RaiaMrcPTLv1 : IDevice
    {
        private byte[] STX_LC = new byte[] { 0x02, 0x02 };       // "\x02\x02";
        private byte[] ETX_LC = new byte[] { 0x03, 0x03 };       // "\x03\x03";
        private byte[] STX_AT = new byte[] { 0x0f, 0x00, 0x60 }; //"\x0F\x00\x60";

        public string Name { get { return Config.Name; } }

        public BackgroundWorker Worker { get; private set; }

        private readonly Dictionary<string, Action<string, object>> tagsAction;
        private readonly object lockSendDataQueue = new object();
        private Queue<(string Id, string Message)> sendDataQueue;
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
        private string msg_rcvd = "";
        private string msg_id = null;
        private string msg_to_send = null;

        private const int RECONNECT_DELAY = 3000;
        public bool Ready { get; private set; }
        public DateTime LastSend { get; private set; }

        byte[] receiveBuffer = new byte[0];

        private object tagsActionLock;

        public bool Enabled { get { return true; } }
        public bool Connected { get { return client?.Connected ?? false; } }

        public DateTime LastErrorTime { get { return DateTime.Now; } }

        public IReadOnlyDictionary<string, object> TagValues { get { return null; } }

        public int LicenseRemainingHours { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime? LastUpdateDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public RaiaMrcPTLv1()
        {
            this.tagsAction = new Dictionary<string, Action<string, object>>();
            this.sendDataQueue = new Queue<(string Id, string Message)>();
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
            if (tagName == "msg_rcvd")
                return msg_rcvd;
            else if (tagName == "msg_id")
                return msg_id;

            return null;
        }

        private readonly List<PtlBaseClass> ListaLigados = new List<PtlBaseClass>();

        public void SetTagValue(string tagName, object value)
        {
            // travar para evivitar problema de concorrencia
            lock (lockSendDataQueue)
            {
                if (string.IsNullOrWhiteSpace((string)value))
                    return;

                if (tagName == "msg_id")
                {
                    msg_id = value.ToString();
                    if (msg_to_send == null)
                        return;
                }

                if (tagName == "msg_to_send")
                {
                    msg_to_send = value.ToString();
                    if (msg_id == null)
                        return;
                }

                // se ja tem msg_id e msg_to_send, coloca na fila

                // se ainda nao tem uma Msg com esta Id na fila
                if (!sendDataQueue.Any(x => x.Id == msg_id))
                {
                    Logger.Info($"Dev {Config.Name}: O Id '{msg_id}', {msg_to_send} adicionada na fila!");
                    sendDataQueue.Enqueue((msg_id, msg_to_send));

                    msg_id = null;
                    msg_to_send = null;
                    return;
                }
                else
                {
                    msg_id = null;
                    msg_to_send = null;

                    Logger.Info($"Dev {Config.Name}: O Id '{msg_id}', ainda esta na fila!");
                    return;
                }
            }
        }

        private bool ReceiveData()
        {
            return false;

            /*var recv = client.GetData();
            if (recv != null && recv.Length > 0)
            {
                // adiciona os dados recebidos ao buffer dos dados ja existens
                Logger.Info($"Dev {Config.Name}: Received: '{recv}'.\tString: '{ASCIIEncoding.ASCII.GetString(recv)}'\t ByteArray: '{string.Join(", ", recv)}'");
                var tempBuffer = new byte[receiveBuffer.Length + recv.Length];
                receiveBuffer.CopyTo(tempBuffer, 0);
                recv.CopyTo(tempBuffer, receiveBuffer.Length);
                receiveBuffer = tempBuffer;

                received = true;
            }

            if (receiveBuffer.Length > 0)
            {
                //var strRcvd = Encoding.ASCII.GetString(receiveBuffer);
                var strRcvd = receiveBuffer;

                byte[] CONFIRM_MSG = new byte[] { 88, 88, 46 }; // XX. 
                var confirmPos = SearchBytes(strRcvd, CONFIRM_MSG);

                Logger.Info($"Dev {Config.Name}: confirmPos {confirmPos} ByteArray: '{string.Join(", ", strRcvd)}'");

                if (confirmPos >= 0) {
                    // incrementa o contador de msg recebida

                    var sendCMD = $"TESTE:" + ASCIIEncoding.ASCII.GetString(CONFIRM_MSG);
                    cmd_rcvd = sendCMD;
                    cmd_count++;

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

                    receiveBuffer = receiveBuffer[(confirmPos)..];
                }
            }

            return received;*/
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
                    Logger.Error($"Dev {Config.Name}: Update Loop Error {ex}");
                }
            }
        }

        private bool SendData()
        {
            var sent = false;

            if (sendDataQueue.Count > 0)
                lock (lockSendDataQueue)
                {
                    var st = new Stopwatch();
                    st.Start();

                    var it = sendDataQueue.Dequeue();
                    
                    var buffer = Encoding.ASCII.GetBytes(it.Message);
                    client.SendData(buffer);

                    sent = true;

                    /*var totalLength = 0;
                    foreach (var it in sendDataQueue)
                    {
                        totalLength += it.Message.Length;
                    }

                    var obj = new byte[totalLength];
                    var pos = 0;
                    while (sendDataQueue.Count > 0)
                    {
                        var it = sendDataQueue.Dequeue();
                        Array.Copy(it, 0, obj, pos, it.Message.Length);
                        pos += it.Length;
                    }

                    client.SendData(obj);*/

                    st.Stop();

                    Logger.Info($"Dev {Config.Name}: Enviado Id {it.Id} Msg {buffer.Length} bytes em {st.ElapsedMilliseconds} ms.");

                    this.LastSend = DateTime.Now;
                }
            else
            {
                /*
                 * hearthbit 
                if (LastSend.AddSeconds(3) < DateTime.Now)
                {
                    var getFwCmd = new byte[] { 0x07, 0x00, 0x60, 0x00, 0x00, 0x00, 0x09 };
                    client.SendData(getFwCmd);
                    this.LastSend = DateTime.Now;
                }*/
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

        public void GetLicenseRemainingHours()
        {
            LicenseRemainingHours = int.MaxValue;
        }
    }
}
