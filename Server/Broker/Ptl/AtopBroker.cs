using NLog;
using Otm.Server.Device.Ptl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using Otm.Server.ContextConfig;

namespace Otm.Server.Broker.Ptl
{
    public class AtopBroker : IPtlAmqtpBroker
    {
        private byte[] STX_LC = new byte[] { 0x02, 0x02 };       // "\x02\x02";
        private byte[] ETX_LC = new byte[] { 0x03, 0x03 };       // "\x03\x03";
        private byte[] STX_AT = new byte[] { 0x0f, 0x00, 0x60 }; //"\x0F\x00\x60";
        private byte[] STX_AT_MASTER_DISP12 = new byte[] { 0x14, 0x00, 0x60 }; //"\x2b\x00\x60";
        private byte[] STX_AT_MASTER_DISP08 = new byte[] { 0x11, 0x00, 0x60 }; //"\x2b\x00\x60";

        private byte MasterDevice;
        private bool readGateOpen;
        private bool hasReadGate;
        private string testCardCode;

        private string cmd_rcvd = "";
        private int cmd_count = 0;

        public AtopBroker(BrokerConfig config, ILogger logger) : base(config, logger)
        {
        }

        public override void displaysOn(IEnumerable<PtlBaseClass> listaAcender)
        {
            foreach (var itemAcender in listaAcender.ToList())
            {
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

                if (itemAcender.Location == Config.MasterDevice)
                {
                    // Apaga o display antes de setar um novo valor
                    // set color { 0x00 -vermelho, 0x01 - verde, 0x02 - laranja, 0x03 - led off}
                    buf2.AddRange(new byte[] { 0x0A, 0x00, 0x60, 0x00, 0x00, 0x00, 0x1f, displayId, 0x00, (byte)E_DisplayColor.Off });
                    // limpa o display
                    buf2.AddRange(new byte[] { 0x08, 0x00, 0x60, 0x00, 0x00, 0x00, 0x01, displayId });

                    //buf2.AddRange(new byte[] { 0x07, 0x00, 0x60, 0x00, 0x00, 0x00, 0x01, 0xFC });

                    /// TODO: TRANSFORMAR ISSO EM UMA CONFIGURAÇÃO, POR TIPO DE DISPLAY
                    if(false) // TIPO DISPLAY 12 DIGITOS
                    {
                    // este comando comentado funciona para o display de 12 digitos
                    // // msgLength = (byte)(msgLength + 1);
                    // buf2.AddRange(new byte[] { msgLength, 0x00, 0x60, 0x66, 0x00, 0x00, 0x00, displayId, 0x11 });
                    // buf2.AddRange(displayCode);
                    // buf2.Add(0x01);
                    }

                    if (true) // TIPO DISPLAY 8 DIGITOS
                    {
                        // comando para o display de 8 digitos
                        buf2.AddRange(new byte[] { msgLength, 0x00, 0x60, 0x00, 0x00, 0x00, 0x00, displayId });
                        buf2.AddRange(displayCode);
                        buf2.Add(0x01);
                    }
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

                lock (lockSendDataQueue)
                {
                    sendDataQueue.Enqueue(buf.ToArray());
                }

                ListaLigados.Add(itemAcender);

                //SendData();
            }
        }

        public void displayOff(IEnumerable<PtlBaseClass> ListaApagar) {
            Logger.Info("displayOff count:" + ListaApagar.Count());
            foreach (var itemApagar in ListaApagar.ToList())
            {
                Logger.Info("displayOff foreach itemApagar:" + itemApagar.DisplayValue);
                
                byte displayId = itemApagar.GetDisplayId();

                Logger.Info("displayOff foreach displayId:" + displayId);
                
                var buffer = new List<byte>();
                // set color { 0x00 -vermelho, 0x01 - verde, 0x02 - laranja, 0x03 - led off}
                buffer.AddRange(new byte[] { 0x0A, 0x00, 0x60, 0x00, 0x00, 0x00, 0x1f, displayId, 0x00, (byte)E_DisplayColor.Off });
                // limpa o display
                buffer.AddRange(new byte[] { 0x08, 0x00, 0x60, 0x00, 0x00, 0x00, 0x01, displayId });
                
                lock (lockSendDataQueue)
                {
                    Logger.Info("Apagar o display" + displayId);
                    sendDataQueue.Enqueue(buffer.ToArray());
                }

                ListaLigados.Remove(itemApagar);
            }
        }

        public override void ProcessMessage(byte[] body)
        {
            var message = Encoding.Default.GetString(body);

            Logger.Info($"ProcessMessage(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Message received: {message}");

            Regex regex = new Regex(@"'(.*?)'");
            string conteudo = regex.Match(message).Groups[1].Value;

            var ListaPendentes = (from rawPendente in (conteudo).Split(',').ToList()
                                  let pententeInfos = rawPendente.Split('|').ToList()
                                  select new PtlBaseClass(id: Guid.NewGuid(),
                                                          location: pententeInfos[0],
                                                          displayColor: (E_DisplayColor)byte.Parse(pententeInfos[1]),
                                                          displayValue: pententeInfos[2])
                                                                ).ToList();

            var listaPendentes = (from rawPendente in (conteudo).Split(',').ToList()
                    let pententeInfos = rawPendente.Split('|').ToList()
                    select new PtlBaseClass(id: Guid.NewGuid(),
                        location: pententeInfos[0],
                        displayColor: (E_DisplayColor)byte.Parse(pententeInfos[1]),
                        displayValue: pententeInfos[2])
                ).ToList();

            var strListaPendentes = listaPendentes.Aggregate("", (s, x) =>
                s +
                " Location: " + x.Location +
                " DisplayValue: " + x.DisplayValue +
                " DisplayColor: " + x.DisplayColor + ", ");
            
            Logger.Info("listaPendentes" + strListaPendentes);

            var strListaLigados = ListaLigados.Aggregate("", (s, x) =>
                s +
                " Location: " + x.Location +
                " DisplayValue: " + x.DisplayValue +
                " DisplayColor: " + x.DisplayColor + ", ");
            
            Logger.Info("listaLigados" + strListaLigados);
            
            var listaApagar = ListaLigados.Where(x1 => !listaPendentes.Any(x2 =>
                x1.Location == x2.Location
                && x1.DisplayValue == x2.DisplayValue
                && x1.DisplayColor == x2.DisplayColor
            ));
            
            var strListaApagar = listaApagar.Aggregate("", (s, x) =>
                s +
                " Location: " + x.Location +
                " DisplayValue: " + x.DisplayValue +
                " DisplayColor: " + x.DisplayColor + ", ");
            
            Logger.Info("listaApagar" + strListaApagar);


            displayOff(listaApagar);
            displaysOn(listaPendentes);
        }

        public override bool ReceiveData()
        {
            var received = false;

            var recv = client.GetData();
            if (recv != null && recv.Length > 0)
            {
                Logger.Info($"ReceiveData(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Received: '{recv}'.\tString: '{ASCIIEncoding.ASCII.GetString(recv)}'\t ByteArray: '{string.Join(", ", recv)}'");
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

                                var message = Encoding.ASCII.GetString(strRcvd[(stxLcPos + STX_LC.Length)..etxLcPos]);
                                var cmdParts = message.Split('|');

                                var cmdType = cmdParts[0];
                                var cmdDevice = cmdParts[1];
                                var cmdValue = cmdParts[2];

                                var sendCMD = $"{Config.Name}|{cmdType}|{cmdDevice}|{cmdValue}";

                                cmd_rcvd = sendCMD;
                                cmd_count++;
                                //received = true;

                                var queueName = Config.AmqpQueueToProduce;

                                Logger.Info($"ReceiveData(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Message received: {message}");
                                
                                Logger.Info($"messageToAmqtp DEBUG -> {Config.AmqpQueueToProduce} | {Config.Name} | {Config.PtlId} | {cmdDevice} | {cmdValue} | {(cmdValue.All(c => c == '0') ? 0 : cmdValue.TrimStart('0'))}");
                            
                                var messageToAmqtp = String.Join(',',
                                    Config.AmqpQueueToProduce,
                                    Config.Name,
                                    $"{Config.PtlId}:{cmdDevice}",
                                    cmdValue.All(c => c == '0') ? 0 : cmdValue.TrimStart('0'),
                                    DateTime.Now
                                );
                                var json = JsonConvert.SerializeObject(new { Body = messageToAmqtp });

                                AmqpChannel.BasicPublish("", queueName, true, basicProperties, Encoding.ASCII.GetBytes(json));

                                readGateOpen = false;

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

                            if (subCmd == 252)
                            {
                                Logger.Info($"ReceiveData(): Device: '{Config.Name}'. subCmd: 252 IGNORADO");
                            }
                            else
                            {
                                var sendCMD = $"{Config.Name}|AT|{subNode:000}|{cmdValue}";
                                // ptl01|AT|001|000001
                                cmd_rcvd = sendCMD;
                                cmd_count++;
                                received = true;

                                var queueName = Config.AmqpQueueToProduce;

                                Logger.Info($"ReceiveData() AT: Drive: '{Config.Driver}'. Device: '{Config.Name}'. Message received: {cmdAT}");
                                Logger.Info($"messageToAmqtp AT DEBUG -> {Config.AmqpQueueToProduce} | {Config.Name} | {Config.PtlId} | {subNode.ToString().PadLeft(3, '0')} | {cmdValue} | {(cmdValue.All(c => c == '0') ? 0 : cmdValue.TrimStart('0'))}");
                                
                                var messageToAmqtp = String.Join(',',
                                    Config.AmqpQueueToProduce,
                                    Config.Name,
                                    $"{Config.PtlId}:{subNode.ToString().PadLeft(3, '0')}",
                                    cmdValue.All(c => c == '0') ? 0 : cmdValue.TrimStart('0'),
                                    DateTime.Now
                                );
                                
                                var json = JsonConvert.SerializeObject(new { Body = messageToAmqtp });

                                AmqpChannel.BasicPublish("", queueName, true, basicProperties, Encoding.ASCII.GetBytes(json));
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

                            Logger.Info($"ReceiveData(): Device: '{Config.Name}'. CmdAT: '{cmdAT}' subCmd:{subCmd} subNode:{subNode} cmdValue:{cmdValue}");

                            var sendCMD = $"{Config.Name}|AT|{subNode:000}|{cmdValue}";
                            // ptl01|AT|001|000001
                            cmd_rcvd = sendCMD;
                            cmd_count++;
                            //received = true;

                            var queueName = Config.AmqpQueueToProduce;

                            Logger.Info($"ReceiveData() AT MASTER: Drive: '{Config.Driver}'. Device: '{Config.Name}'. Message received: {cmdAT}");
                            
                            Logger.Info($"messageToAmqtp AT MASTER -> {Config.AmqpQueueToProduce} | {Config.Name} | {Config.PtlId} | {(subNode.ToString().PadLeft(3, '0'))} | {cmdValue} | {(cmdValue.All(c => c == '0') ? 0 : cmdValue.TrimStart('0'))}");
                            
                            var messageToAmqtp = String.Join(',',
                                Config.AmqpQueueToProduce,
                                Config.Name,
                                $"{Config.PtlId}:{(subNode.ToString().PadLeft(3, '0'))}",
                                cmdValue.All(c => c == '0') ? 0 : cmdValue.TrimStart('0'),
                                DateTime.Now
                            );
                            
                            var json = JsonConvert.SerializeObject(new { Body = messageToAmqtp });

                            AmqpChannel.BasicPublish("", queueName, true, basicProperties, Encoding.ASCII.GetBytes(json));


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

        public override byte[] GetMessagekeepAlive()
        {
            return new byte[] { 0x07, 0x00, 0x60, 0x00, 0x00, 0x00, 0x09 };
        }
    }
}