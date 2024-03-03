using Newtonsoft.Json;
using NLog;
using Otm.Server.Device.Ptl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Otm.Server.ContextConfig;

namespace Otm.Server.Broker.Ptl
{
    public class SmartPickingBroker : IPtlAmqtpBroker
    {
        public SmartPickingBroker(BrokerConfig config, ILogger logger) : base(config, logger)
        {
        }

        private byte[] PRS_START = Encoding.ASCII.GetBytes("P");
        private byte[] PONG_START = Encoding.ASCII.GetBytes("A");
        
        private byte[] pongChars = Encoding.ASCII.GetBytes("ACKPONG|PING");
        private byte[] prsChars = Encoding.ASCII.GetBytes("PRS");
        
        private int messageSize = 12;
        private int PRS_SIZE = 13;
        private int PONG_SIZE = 12;
        public DateTime LastReceive { get; set; }

        public override void ProcessMessage(byte[] body) {

            var message = Encoding.Default.GetString(body);

            Logger.Info($"ProcessMessage(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Message received: {message}");

            Regex regex = new Regex(@"'(.*?)'");
            string conteudo = regex.Match(message).Groups[1].Value;

            var ListaPendentes = (from rawPendente in (conteudo).Split(',').ToList()
                                  let pententeInfos = rawPendente.Split('|').ToList()
                                    select new PtlBaseClass(id: Guid.Parse(pententeInfos[3]),
                                                            location: pententeInfos[0],
                                                            displayColor: pententeInfos[1],
                                                            displayValue: pententeInfos[2])
                                                                ).ToList();

                           
            var ListaAcender = ListaPendentes.Where(pendente => !ListaLigados.Any(ligado =>
                ligado.Location == pendente.Location 
                && ligado.DisplayValue == pendente.DisplayValue 
                && ligado.Id == pendente.Id
            ));

            //var ListaApagar = ListaLigados.Where(ligado => !ListaPendentes.Any(pendente => pendente.Location == ligado.Location && pendente.DisplayValue == ligado.DisplayValue || pendente.DisplayColorInt != ligado.DisplayColorInt));
            var ListaApagar = ListaLigados.Where(ligado => !ListaPendentes.Any(pendente =>
                pendente.Location == ligado.Location 
                && pendente.DisplayValue == ligado.DisplayValue
                && pendente.Id == ligado.Id
                || pendente.DisplayColorInt != ligado.DisplayColorInt
            ));

            displayOff(ListaApagar);

            displaysOn(ListaAcender);

        }

        public void displayOff(IEnumerable<PtlBaseClass> ListaApagar)
        {
            foreach (var itemApagar in ListaApagar.ToList())
            {
                int displayId = itemApagar.GetDisplayIdToInt();

                var mensagem = String.Format("{0:D3}{1:D3}|", "CLR", displayId);

                var buffer = System.Text.Encoding.ASCII.GetBytes(mensagem);

                sendDataQueue.Enqueue(buffer.ToArray());
                ListaLigados.Remove(itemApagar);
                SendData();
            }
        }

        public override void displaysOn(IEnumerable<PtlBaseClass> listaAcender)
        {
            foreach (var itemAcender in listaAcender.ToList())
            {
                string mensagem;

                var displayId = itemAcender.GetDisplayIdToInt();


                if (itemAcender.Location == Config.MasterDevice)
                {                    
                    mensagem = String.Format("{0:D3}{1:D3}{2:D6}|", "SHW", displayId, itemAcender.DisplayValue);
                }
                else
                {
                    int parsedValue;
                    if (int.TryParse(itemAcender.DisplayValue, out parsedValue))
                    {
                        mensagem = String.Format("{0:D3}{1:D3}{2:D3}{3:D1}{4:D1}|", "SHW", displayId, parsedValue, itemAcender.DisplayColorInt, 1);
                    }
                    else
                    {
                        mensagem = String.Format("{0:D3}{1:D3}{2:D6}|", "SHW", displayId, itemAcender.DisplayValue);
                    }
                    //mensagem = String.Format("{0:D3}{1:D3}{2:D3}{3:D1}{4:D1}|", "SHW", displayId, itemAcender.DisplayValue, itemAcender.DisplayColorInt, 1);
                }

                var buffer = System.Text.Encoding.ASCII.GetBytes(mensagem);

                sendDataQueue.Enqueue(buffer.ToArray());

                ListaLigados.Add(itemAcender);

                SendData();
            }
        }

        public override bool ReceiveData()
        {
            var recv = client.GetData();
            var received = false;
            
            // verifica se recebeu algo nos ultimos 5 segundos
            if (LastReceive.AddSeconds(5) < DateTime.Now)
            {
                // se nao recebeu, desconecta
                client.Dispose();
            }

            // se recebeu algo, adiciona ao buffer
            if (recv != null && recv.Length > 0)
            {
                var tempBuffer = new byte[receiveBuffer.Length + recv.Length];
                receiveBuffer.CopyTo(tempBuffer, 0);
                recv.CopyTo(tempBuffer, receiveBuffer.Length);
                receiveBuffer = tempBuffer;
            }

            if (receiveBuffer.Length > 0)
            {
                if (receiveBuffer.Length >= PRS_SIZE)
                {
                    var prsPos = SearchBytes(receiveBuffer, prsChars);

                    // se nao tem PRS no buffer, verifica se tem PONG, retira do buffer e retorna false
                    if (prsPos < 0)
                    {
                        
                        if (receiveBuffer.Length >= PONG_SIZE)
                        {
                            var pongPos = SearchBytes(receiveBuffer, pongChars);
                    
                            if (pongPos >= 0)
                            {
                                // remove do receiveBuffer
                                receiveBuffer = receiveBuffer[(pongPos + PONG_SIZE-1)..];
                            }
                        }
                        
                        return false;
                    }

                }
                



                LastReceive = DateTime.Now;
                
                var strRcvd = receiveBuffer;

                var stxLcPos = SearchBytes(strRcvd, PRS_START);

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
                    if (receiveBuffer.Length >= primeiraPosRelevante + messageSize)
                    {
                        received = true;
                        var msgBytes = receiveBuffer[primeiraPosRelevante..(primeiraPosRelevante + messageSize)];
                        // Exemploe de msg PRS999888888
                        var message = Encoding.ASCII.GetString(msgBytes);
                        Logger.Info(message);
                        if (message.StartsWith("PRS")) { 
                            string prefixo = message.Substring(0, 3);
                            string display = message.Substring(3, 3);
                            string value = message.Substring(6, 6);
                            string endereco = $"{Config.PtlId}:{display}";

                            //var itemApagar = ListaLigados.Where(x => x.Location == endereco).ToList();
                            //foreach (var item in itemApagar) { 
                            //    ListaLigados.Remove(item);
                            //}


                            var queueName = Config.AmqpQueueToProduce;

                            Logger.Info($"ReceiveData(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Message received: {message}");

                            var messageToAmqtp = String.Join(',',
                                Config.AmqpQueueToProduce,
                                Config.Name,
                                $"{Config.PtlId}:{display}",
                                value.All(c => c == '0') ? 0 : value.TrimStart('0'),
                                DateTime.Now
                            );

                            Logger.Info($"ReceiveData(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Message send: {messageToAmqtp}");

                            var json = JsonConvert.SerializeObject(new { Body = messageToAmqtp });

                            AmqpChannel.BasicPublish("", queueName, true, basicProperties, Encoding.ASCII.GetBytes(json));                        
                        }

                        receiveBuffer = receiveBuffer[(primeiraPosRelevante + messageSize)..];
                    }
                }
                else
                    receiveBuffer = Array.Empty<byte>();
            }

            return received;
        }



        public DateTime LastPingSend { get; set; }
        /// <summary>
        /// Envio de ping para o controlador se não tiver enviado um ping a menos de 2 segundos
        /// </summary>
        public override void SendPing()
        {
            // se já enviou um ping a menos de 2 segundos, não envia outro
            if (LastPingSend.AddSeconds(2) > DateTime.Now)
                return;
            
            // envia um ping para o controlador
            var pingMsg = "PING|";
            var bytes = Encoding.ASCII.GetBytes(pingMsg);
            client.SendData(bytes);
            LastPingSend = DateTime.Now;
        }
        
        
        
        public override byte[] GetMessagekeepAlive()
        {
            return System.Text.Encoding.ASCII.GetBytes("PONG");
        }
    }
}
