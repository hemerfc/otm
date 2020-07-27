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
        private const char STX = '\u0002';
        private const char ETX = '\u0003';
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

            return null;
        }

        public void SetTagValue(string tagName, object value)
        {
            if (tagName != "cmd_send")
                Logger.LogError($"Dev {Config.Name}: O tag name '{tagName}', não é valido!");
            else if (value is string)
            {
                var buffer = Encoding.UTF8.GetBytes((string)value);
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
            byte[] receiveBuffer = new byte[0];

            while (true)
            {
                try
                {
                    if (client.Connected)
                    {
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
                            var stxPos = strRcvd.IndexOf(STX);
                            var etxPos = strRcvd.IndexOf(ETX);

                            if (etxPos < 0 && stxPos > 0)
                                receiveBuffer = receiveBuffer[stxPos..];
                            if (etxPos < stxPos)
                                receiveBuffer = receiveBuffer[stxPos..];
                            else if (stxPos < etxPos)
                            {
                                if (receiveBuffer.Length > etxPos + 1)
                                    receiveBuffer = receiveBuffer[(etxPos + 1)..];
                                else
                                    receiveBuffer = new byte[0];

                                var cmd = strRcvd[(stxPos + 1)..etxPos];

                                cmd_rcvd = cmd;
                                cmd_count++;

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

                        if (sendDataQueue.Count > 0)
                            lock (lockSendDataQueue)
                            {
                                while (sendDataQueue.Count > 0)
                                {
                                    var obj = sendDataQueue.Dequeue();
                                    client.SendData(obj);
                                }
                            }
                    }
                    else
                    {
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
                    // wait 100ms
                    /// TODO: wait time must be equals the minimum update rate of tags
                    var waitEvent = new ManualResetEvent(false);
                    waitEvent.WaitOne(100);

                    if (Worker.CancellationPending)
                    {
                        Stop();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Dev {Config.Name}: Update Loop Error {ex.ToString()}");
                }
            }
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
