using Microsoft.Extensions.Logging;
using Otm.Server.Device;
using Otm.Server.Device.Ptl;
using Otm.Shared.ContextConfig;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Otm.Plugins.Devices.RaiaMrcPTL
{
    /// <summary>
    /// Device que envia mensagens (socket) para o WMS Manhatan da Raia
    /// sempre que um picking do PTL for terminado
    /// </summary>
    public class RaiaMrcSocketDevice : IDevice
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
        public bool Connected { get { return client?.Connected ?? false; } }

        public DateTime LastErrorTime { get { return DateTime.Now; } }

        public IReadOnlyDictionary<string, object> TagValues { get { return null; } }

        public RaiaMrcSocketDevice()
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
            else if (tagName == "wcs_ctrl")
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

                // preparar a mensagen aki...
                var buffer = Encoding.ASCII.GetBytes((string)value);

                // travar e colocar na fila...
                lock (lockSendDataQueue)
                {
                    sendDataQueue.Enqueue(buffer.ToArray());
                }
            }
        }

        private bool ReceiveData()
        {
            // acho que nao deve recever nada....
            return false;
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
