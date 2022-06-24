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
using Otm.Server.Device.Licensing;
using Newtonsoft.Json;

namespace Otm.Server.Device.Ptl
{
    public class PtlDevice : IDevice
    {
        private byte[] STX_LC = new byte[] { 0x02, 0x02 };       // "\x02\x02";
        private byte[] ETX_LC = new byte[] { 0x03, 0x03 };       // "\x03\x03";
        private byte[] STX_AT = new byte[] { 0x0f, 0x00, 0x60 }; //"\x0F\x00\x60";
        private byte[] STX_AT_MASTER_DISP12 = new byte[] { 0x14, 0x00, 0x60 }; //"\x2b\x00\x60";
        private byte[] STX_AT_MASTER_DISP08 = new byte[] { 0x11, 0x00, 0x60 }; //"\x2b\x00\x60";

        public string Name { get { return Config.Name; } }

        public BackgroundWorker Worker { get; private set; }

        private readonly Dictionary<string, Action<string, object>> tagsAction;
        private readonly object lockSendDataQueue = new object();
        private Queue<byte[]> sendDataQueue;
        private ILogger Logger;
        private DeviceConfig Config;
        private ITcpClientAdapter client;
        private string ip;
        private string licenseKey;
        private int port;

        private byte MasterDevice;
        private bool readGateOpen;
        private bool hasReadGate;
        private string testCardCode;

        //readonly bool firstLoadRead;
        //readonly bool firstLoadWrite;
        private bool Connecting;
        private DateTime lastConnectionTry;
        private static DateTime? LastLicenseTry;
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

        #region License
        public int LicenseRemainingHours { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        #endregion

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

                Logger.LogInformation($"SetTagValue(): PickTolight visualization: '{Config.Name}'. value: '{value}'");

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

            GetLicenseRemainingHours();

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
                var stxAtMasterPos12 = SearchBytes(strRcvd, STX_AT_MASTER_DISP12);
                var stxAtMasterPos8 = SearchBytes(strRcvd, STX_AT_MASTER_DISP08);


                var posicoesRelevantesEncontradas = new List<int>() { stxLcPos, stxAtPos, stxAtMasterPos12, stxAtMasterPos8 }
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
                            if ((!hasReadGate) || (hasReadGate && readGateOpen))
                            {

                                var cmdLC = Encoding.ASCII.GetString(strRcvd[(stxLcPos + STX_LC.Length)..etxLcPos]);
                                var cmdParts = cmdLC.Split('|');

                                var cmdType = cmdParts[0];
                                var cmdDevice = cmdParts[1];
                                var cmdValue = cmdParts[2];

                                var sendCMD = $"{Config.Name}|{cmdType}|{cmdDevice}|{cmdValue}";

                                cmd_rcvd = sendCMD;
                                cmd_count++;
                                //received = true;

                                //Se o valor lido for o mesmo do test card code, envia o comando pra testar o PTL
                                if (cmdValue == testCardCode)
                                {
                                    EnviarComandoTeste();
                                }
                                else
                                {
                                    if (tagsAction.ContainsKey("cmd_rcvd"))
                                    {
                                        tagsAction["cmd_rcvd"]("cmd_rcvd", cmd_rcvd);
                                    }
                                    else if (tagsAction.ContainsKey("cmd_count"))
                                    {
                                        tagsAction["cmd_count"]("cmd_count", cmd_count);
                                    }
                                }

                                EnviarBipLeituraMestre(MasterDevice);
                                readGateOpen = false;
                                Logger.LogInformation($"ReceiveData(): Device: '{Config.Name}'. readGate fechado após leitura.");

                            }
                            //Se o ReadGate estava aberto a leitura foi processada, se estava fechado ignorada...
                            //Descartando a leitura do buffer pois ja foi processada
                            //received = true;
                            receiveBuffer = receiveBuffer[(etxLcPos + STX_LC.Length)..];
                        }
                    }
                    else if (primeiraPosRelevante == stxAtPos) //Se for um atendimento normal, verifica se ja tem 15 posicoes pra frente
                    {
                        var len = 15;

                        //verifica se ja tem 20 posicoes pra frente e processa
                        if (strRcvd.Length >= stxAtPos + len)
                        {

                            var cmdAT = strRcvd[stxAtPos..(stxAtPos + len)];

                            var subCmd = cmdAT[6];
                            var subNode = cmdAT[7];
                            var cmdValue = Encoding.ASCII.GetString(cmdAT.Skip(8).Take(6).ToArray());

                            Logger.LogInformation($"ReceiveData(): Device: '{Config.Name}'. CmdAT: '{cmdAT}' subCmd:{subCmd} subNode:{subNode} cmdValue:{cmdValue}");

                            if (subCmd == 252)
                            {
                                Logger.LogInformation($"ReceiveData(): Device: '{Config.Name}'. subCmd: 252 IGNORADO");
                            }
                            else
                            {
                                var sendCMD = $"{Config.Name}|AT|{subNode:000}|{cmdValue}";
                                // ptl01|AT|001|000001
                                cmd_rcvd = sendCMD;
                                cmd_count++;
                                //received = true;

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

                            //Limpando o buffer que ja foi processado
                            //received = true;
                            receiveBuffer = receiveBuffer[(stxAtPos + len)..];
                        }
                    }
                    else if ((primeiraPosRelevante == stxAtMasterPos12) || (primeiraPosRelevante == stxAtMasterPos8)) //Se for um atendimento master
                    {
                        var posMaster = (primeiraPosRelevante == stxAtMasterPos12) ? stxAtMasterPos12 : stxAtMasterPos8;
                        var len = (primeiraPosRelevante == stxAtMasterPos12) ? 20 : 17;

                        //verifica se ja tem 20 posicoes pra frente e processa
                        if (strRcvd.Length >= posMaster + len)
                        {
                            readGateOpen = true;

                            var cmdAT = strRcvd[posMaster..(posMaster + len)];

                            var subCmd = cmdAT[6];
                            var subNode = cmdAT[7];
                            var cmdValue = Encoding.ASCII.GetString(cmdAT.Skip(8).Take(6).ToArray());

                            Logger.LogInformation($"ReceiveData(): Device: '{Config.Name}'. CmdAT: '{cmdAT}' subCmd:{subCmd} subNode:{subNode} cmdValue:{cmdValue}");




                            var sendCMD = $"{Config.Name}|AT|{subNode:000}|{cmdValue}";
                            // ptl01|AT|001|000001
                            cmd_rcvd = sendCMD;
                            cmd_count++;
                            //received = true;

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


                            //Limpando o buffer que ja foi processado
                            //received = true;
                            receiveBuffer = receiveBuffer[(posMaster + len)..];
                        }
                    }
                }
                else
                    receiveBuffer = Array.Empty<byte>();


            }

            return received;
        }

        private void EnviarBipLeituraMestre(byte MasterDevice)
        {
            Logger.LogInformation($"ReceiveData(): Device: '{Config.Name}'. enviando BIP para o display mestre {MasterDevice}.");
        }

        private void EnviarComandoTeste()
        {
            Logger.LogInformation($"EnviarComandoTeste(): Device: '{Config.Name}'. enviando Comando teste para o controlador.");
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

        public void GetLicenseRemainingHours()
        {


            /*Atualiza quando:
             * - Na inicialização do serviço
             * - Quando a data da última atualização tiver 3 dias de diferença da última. Implementei com Mod, caso for alterada a data do servidor para frente, também funciona
             */

            //Temporário até corrigir o container
            LicenseRemainingHours = int.MaxValue;
            /*
            if (LastLicenseTry == null
                || LastUpdateDate == null
                || Math.Abs((LastUpdateDate.Value - DateTime.Now).TotalDays) >= 3
                )
            {
                try
                {
                    Logger.LogInformation($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | Obtendo licenca...");

                    string HostIdentifier = Environment.MachineName;
                    //Temporariamente pegando o Ip, deve-se tentar pegar algo imutavel do device
                    string DeviceIdentifier = this.ip; //MacAddress.getMacByIp(this.ip);
                    //string DeviceIdentifier = MacAddress.getMacByIp("192.168.15.43");

                    string LicenseKey = this.licenseKey;

                    Logger.LogInformation($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | HostIdentifier: { HostIdentifier}");
                    Logger.LogInformation($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | MacAddress: { DeviceIdentifier}");
                    Logger.LogInformation($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | LicenseKey {LicenseKey}");
                    
                    LicenseRemainingHours = new Licensing.License(HostIdentifier, DeviceIdentifier, LicenseKey).GetRemainingHours();

                    Logger.LogInformation($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | Licenca obtida, restante {LicenseRemainingHours} horas.");
                    
                    LastUpdateDate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Logger.LogInformation($"PtlDevice | {Config.Name} | GetLicenseRemainingDays | Error:  {ex}");
                }
                LastLicenseTry = DateTime.Now;
            }
            */

        }
    }
}
