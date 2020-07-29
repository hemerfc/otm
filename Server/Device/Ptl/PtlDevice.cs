using System;
using System.Linq;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Otm.Shared.ContextConfig;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Otm.Server.Device.Ptl
{
    public class PtlDevice : IDevice
    {
        private const string STX_LC = "`GSTX";
        private const string ETX_LC = "ETX";
        private const string STX_AT = "\x0F\x00\x60";

        public string Name { get { return Config.Name; } }

        public BackgroundWorker Worker { get; private set; }

        private readonly Dictionary<string, Action<string, object>> tagsAction;
        private readonly object lockSendDataQueue = new object();
        private Queue<byte[]> sendDataQueue;
        private readonly ILogger Logger;
        private readonly DeviceConfig Config;
        private readonly ITcpClientAdapter client;
        private string ip;
        private int port;
        //readonly bool firstLoadRead;
        //readonly bool firstLoadWrite;
        private bool Connecting;
        private DateTime lastConnectionTry;
        private string cmd_rcvd = "";
        private int cmd_count = 0;
        private const int RECONNECT_DELAY = 5000;
        public bool Ready { get; private set; }
        byte[] receiveBuffer = new byte[0];

        public PtlDevice(DeviceConfig dvConfig, ITcpClientAdapter client, ILogger logger)
        {
            this.Logger = logger;
            this.Config = dvConfig;
            this.client = client;
            this.tagsAction = new Dictionary<string, Action<string, object>>();
            this.sendDataQueue = new Queue<byte[]>();

            GetConfig(dvConfig);
            //firstLoadRead = true;
            //firstLoadWrite = true;
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

        private readonly List<ReceivePTLViewModel> ListaLigados = new List<ReceivePTLViewModel>();

        public void SetTagValue(string tagName, object value)
        {
            if (tagName != "cmd_send")
                Logger.LogError($"Dev {Config.Name}: O tag name '{tagName}', não é valido!");
            else if (value is string @string)
            {
                // recebeu do datapoint 
                // criar os comandos pro PTL

                // 001:002|01|00000000001|pick;001:003|01|ITEM OK|msg
                var ListaPendentes = (from rawPendente in ((string)value).Split(';').ToList()
                                      let pententeInfos = rawPendente.Split('|').ToList()
                                      select new ReceivePTLViewModel(location: pententeInfos[0],
                                                                    displayColor: (E_DisplayColor)byte.Parse(pententeInfos[1]),
                                                                    displayValue: pententeInfos[2],
                                                                    messageType: (E_PtlMessageType)int.Parse(pententeInfos[3]))).ToList();

                var buffer = Encoding.UTF8.GetBytes(@string);
                lock (lockSendDataQueue)
                {
                    sendDataQueue.Enqueue(buffer);
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
                var tempBuffer = new byte[receiveBuffer.Length + recv.Length];
                receiveBuffer.CopyTo(tempBuffer, 0);
                recv.CopyTo(tempBuffer, receiveBuffer.Length);
                receiveBuffer = tempBuffer;
            }

            if (receiveBuffer.Length > 0)
            {
                var strRcvd = Encoding.ASCII.GetString(receiveBuffer);

                /// TODO: falta processar os bytes recebidos
                var stxLcPos = strRcvd.IndexOf(STX_LC, StringComparison.Ordinal);
                var etxLcPos = strRcvd.IndexOf(ETX_LC, StringComparison.Ordinal);
                var stxAtPos = strRcvd.IndexOf(STX_AT, StringComparison.Ordinal);

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

                        var cmdLC = strRcvd[(stxLcPos + STX_LC.Length)..etxLcPos];
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
                    if (stxAtPos >= 0)
                    {
                        // processa o AT
                        var len = 15; // \x0F\x00\x60

                        var cmdAT = strRcvd[stxAtPos..(stxAtPos + len)];

                        // remove o commando do buffer
                        receiveBuffer = receiveBuffer[(stxAtPos + len)..];

                        var subCmd = (byte)cmdAT[6];
                        var subNode = (byte)cmdAT[7];
                        var cmdValue = cmdAT.Substring(8, 6);

                        var sendCMD = $"{Config.Name}|AT|{subNode:000}|{cmdValue}";
                        // ptl01|AT|001|000001
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
            }

            return received;
        }

        private bool SendData()
        {
            var sent = false;

            if (sendDataQueue.Count > 0)
                lock (lockSendDataQueue)
                {
                    while (sendDataQueue.Count > 0)
                    {
                        var obj = sendDataQueue.Dequeue();
                        client.SendData(obj);
                        sent = true;
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
                    Logger.LogError($"Dev {Config.Name}: Connected.");
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
