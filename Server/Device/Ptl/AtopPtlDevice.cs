using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Server.Device.Ptl
{
    public class AtopPtlDevice : IPtlDevice
    {
        private byte[] STX_LC = new byte[] { 0x02, 0x02 };       // "\x02\x02";
        private byte[] ETX_LC = new byte[] { 0x03, 0x03 };       // "\x03\x03";
        private byte[] STX_AT = new byte[] { 0x0f, 0x00, 0x60 }; //"\x0F\x00\x60";
        private byte[] STX_AT_MASTER_DISP12 = new byte[] { 0x14, 0x00, 0x60 }; //"\x2b\x00\x60";
        private byte[] STX_AT_MASTER_DISP08 = new byte[] { 0x11, 0x00, 0x60 }; //"\x2b\x00\x60";

        public override void ApagarDisplays(IEnumerable<PtlBaseClass> listaApagar)
        {
            foreach (var itemApagar in listaApagar.ToList())
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
        }

        public override void AcenderDisplays(IEnumerable<PtlBaseClass> listaAcender)
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

        public override bool Received(byte[] recv)
        {
            var received = false;

            if (recv != null && recv.Length > 0)
            {
                Logger.LogInformation($"ReceiveData(): Hardware: Atop Drive: '{Config.Driver}'. Device: '{Config.Name}'. Received: '{recv}'.\tString: '{ASCIIEncoding.ASCII.GetString(recv)}'\t ByteArray: '{string.Join(", ", recv)}'");
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

                                if (cmd_rcvd == testCardCode)
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
    }
}
