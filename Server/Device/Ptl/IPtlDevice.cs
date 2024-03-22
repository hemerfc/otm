using System;
using System.Linq;
using System.ComponentModel;
using Otm.Server.ContextConfig;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Diagnostics;
using Otm.Server.Device.Licensing;
using NLog;
using Otm.Server.Device.TcpServer;

namespace Otm.Server.Device.Ptl
{
    public abstract class IPtlDevice : IDevice
    {
        public abstract void ApagarDisplays(IEnumerable<PtlBaseClass> listaApagar);
        public abstract void AcenderDisplays(IEnumerable<PtlBaseClass> listaAcender);
        public abstract bool Received(byte[] recv);

        public string Name { get { return Config.Name; } }

        public BackgroundWorker Worker { get; private set; }

        public readonly Dictionary<string, Action<string, object>> tagsAction;
        public readonly object lockSendDataQueue = new object();
        public Queue<byte[]> sendDataQueue;
        public ILogger Logger;
        public DeviceConfig Config;
        public ITcpClientAdapter client;
        public string ip;
        public string licenseKey;
        public int port;

        public byte MasterDevice;
        public bool readGateOpen;
        public bool hasReadGate;
        public string testCardCode;

        //readonly bool firstLoadRead;
        //readonly bool firstLoadWrite;
        public bool Connecting;
        public DateTime lastConnectionTry;
        public static DateTime? LastLicenseTry;
        public string cmd_rcvd = "";
        public int cmd_count = 0;
        public const int RECONNECT_DELAY = 3000;



        public bool Ready { get; private set; }
        public DateTime LastSend { get; private set; }

        public byte[] receiveBuffer = new byte[0];

        public object tagsActionLock;

        public bool Enabled { get { return true; } }
        public bool Connected { get { return client?.Connected ?? false; } }

        public DateTime LastErrorTime { get { return DateTime.Now; } }

        public IReadOnlyDictionary<string, object> TagValues { get { return null; } }

        #region License
        public int LicenseRemainingHours { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        #endregion

        public IPtlDevice()
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
            this.licenseKey = (cparts.FirstOrDefault(x => x.Contains("key=")) ?? "").Replace("key=", "").Trim();
            var strRack = (cparts.FirstOrDefault(x => x.Contains("port=")) ?? "").Replace("port=", "").Trim();

            var hasReadGateParam = (cparts.FirstOrDefault(x => x.Contains("HasReadGate=")) ?? "").Replace("HasReadGate=", "").Trim();

            if (string.IsNullOrWhiteSpace(hasReadGateParam))
                hasReadGateParam = "false";

            hasReadGate = bool.Parse(hasReadGateParam);

            testCardCode = (cparts.FirstOrDefault(x => x.Contains("TestCardCode=")) ?? "").Replace("TestCardCode=", "").Trim();

            if (string.IsNullOrWhiteSpace(testCardCode))
                testCardCode = "i88888888";

            var masterDeviceParam = (cparts.FirstOrDefault(x => x.Contains("MasterDevice=")) ?? "").Replace("MasterDevice=", "").Trim();

            if (string.IsNullOrWhiteSpace(masterDeviceParam))
                masterDeviceParam = "255";

            this.MasterDevice = Byte.Parse(masterDeviceParam);

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

        public readonly List<PtlBaseClass> ListaLigados = new List<PtlBaseClass>();

        public void SetTagValue(string tagName, object value)
        {
            if (tagName != "cmd_send")
                Logger.Error($"Dev {Config.Name}: O tag name '{tagName}', não é valido!");
            else if (value is string @string)
            {
                if (string.IsNullOrWhiteSpace((string)value))
                    return;
                // recebeu do datapoint 
                // criar os comandos pro PTL

                Logger.Info($"SetTagValue(): PickTolight visualization: '{Config.Name}'. value: '{value}'");

                var ListaPendentes = (from rawPendente in ((string)value).Split(';').ToList()
                                      let pententeInfos = rawPendente.Split('|').ToList()
                                      select new PtlBaseClass(id: Guid.Parse(pententeInfos[4]),
                                                                    location: pententeInfos[0],
                                                                    displayColor: (E_DisplayColor)byte.Parse(pententeInfos[1]),
                                                                    displayValue: pententeInfos[2],
                                                                    displayModel: pententeInfos[3],
                                                                    masterMessage: (E_PTLMasterMessage)int.Parse(pententeInfos[4]))
                                                                    ).ToList();

                //Monta a lista do que é novo                                
                var ListaAcender = ListaPendentes.Where(i => !ListaLigados.Select(x => x.Id).Contains(i.Id));
                //Monsta a lista do que foi removido ou se for mensagem de 30 segundos atras
                var ListaApagar = ListaLigados.Where(i => !ListaPendentes.Select(x => x.Id).Contains(i.Id));

                lock (lockSendDataQueue)
                {
                    ApagarDisplays(ListaApagar);

                    AcenderDisplays(ListaAcender);

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

            //GetLicenseRemainingHours();

            LicenseRemainingHours = 350;
            while (LicenseRemainingHours > 0)
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

        private bool ReceiveData()
        {
            var recv = client.GetData();
            return Received(recv);
        }

        public void EnviarBipLeituraMestre(byte MasterDevice)
        {
            Logger.Info($"ReceiveData(): Device: '{Config.Name}'. enviando BIP para o display mestre {MasterDevice}.");
        }

        public void EnviarComandoTeste()
        {
            Logger.Info($"EnviarComandoTeste(): Device: '{Config.Name}'. enviando Comando teste para o controlador.");
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

                    Logger.Debug($"SendData(): Device: {Config.Name}: Enviado {obj.Length} bytes em {st.ElapsedMilliseconds} ms.");

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

        public void GetLicenseRemainingHours()
        {


            /*Atualiza quando:
             * - Na inicialização do serviço
             * - Quando a data da última atualização tiver 3 dias de diferença da última. Implementei com Mod, caso for alterada a data do servidor para frente, também funciona
             */

            //Temporário até corrigir o container
            //LicenseRemainingHours = int.MaxValue;
            
            if (LastLicenseTry == null
                || LastUpdateDate == null
                || Math.Abs((LastUpdateDate.Value - DateTime.Now).TotalDays) >= 3
                )
            {
                try
                {
                    Logger.Info($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | Obtendo licenca...");

                    string HostIdentifier = Environment.MachineName;
                    //Temporariamente pegando o Ip, deve-se tentar pegar algo imutavel do device
                    string DeviceIdentifier = this.ip; //MacAddress.getMacByIp(this.ip);
                    //string DeviceIdentifier = MacAddress.getMacByIp("192.168.15.43");

                    string LicenseKey = this.licenseKey;

                    Logger.Info($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | HostIdentifier: { HostIdentifier}");
                    Logger.Info($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | MacAddress: { DeviceIdentifier}");
                    Logger.Info($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | LicenseKey {LicenseKey}");
                    
                    LicenseRemainingHours = new Licensing.License(HostIdentifier, DeviceIdentifier, LicenseKey).GetRemainingHours();

                    Logger.Info($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | Licenca obtida, restante {LicenseRemainingHours} horas.");
                    
                    LastUpdateDate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Logger.Info($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | Error:  {ex}");
                }
                LastLicenseTry = DateTime.Now;
            }            
        }

        public void statusPtl() { 
            
        }
    }
}
