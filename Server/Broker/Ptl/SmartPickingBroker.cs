using NLog;
using Otm.Server.Device.Ptl;
using Otm.Server.ContextConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Otm.Server.Broker.Ptl
{
    public class SmartPickingBroker : IPtlAmqtpBroker
    {
        #region init
        public SmartPickingBroker(BrokerConfig config, ILogger logger) : base(config, logger)
        {
        }

        private byte[] STX_LC = Encoding.ASCII.GetBytes("P");
        private int messageSize = 12;
        #endregion
        
        // processa as mensagens recebidas do rabbitmq
        public override void ProcessMessage(byte[] body) 
        {
            var message = Encoding.Default.GetString(body);

            Logger.Info($"ProcessMessage(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Message received: {message}");

            Regex regex = new Regex(@"'(.*?)'");
            string conteudo = regex.Match(message).Groups[1].Value;

            var listaPendentes = (from rawPendente in (conteudo).Split(',').ToList()
                                  let pententeInfos = rawPendente.Split('|').ToList()
                                    select new PtlBaseClass(id: Guid.NewGuid(),
                                                            location: pententeInfos[0],
                                                            displayColor: (E_DisplayColor)byte.Parse(pententeInfos[1]),
                                                            displayValue: pententeInfos[2])
                                                                ).ToList();
            
            var listaApagar = ListaLigados.Where(x => !listaPendentes.Any(x =>
                x.Location == x.Location
                && x.DisplayValue == x.DisplayValue
                && x.DisplayColor == x.DisplayColor
            ));

            displayOff(listaApagar);
            displaysOn(listaPendentes);

            SendData();
        }

        // insere os displays que devem ser apagados na fila de envio (sendDataQueue)
        public void displayOff(IEnumerable<PtlBaseClass> listaApagar)
        {
            foreach (var itemApagar in listaApagar.ToList())
            {
                byte displayId = itemApagar.GetDisplayIdBroker();

                var mensagem = String.Format("{0:D3}{1:D3}|", "CLR", Int32.Parse(itemApagar.Location));

                var buffer = Encoding.ASCII.GetBytes(mensagem);
                
                lock (lockSendDataQueue)
                {
                    sendDataQueue.Enqueue(buffer.ToArray());
                }
                
                ListaLigados.Remove(itemApagar);
            }
        }

        // insere os displays que devem ser ligados na fila de envio (sendDataQueue)
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
                    mensagem = String.Format("{0:D3}{1:D3}{2:D3}{3:D1}{4:D1}|", "SHW", displayId, Int32.Parse(itemAcender.DisplayValue), 7, 0);
                }

                var buffer = System.Text.Encoding.ASCII.GetBytes(mensagem);
                
                lock (lockSendDataQueue)
                {
                    sendDataQueue.Enqueue(buffer.ToArray());
                }
                
                ListaLigados.Add(itemAcender);
            }
        }

        public override bool ReceiveData()
        {
            var recv = client.GetData();
            var received = false;

            if (recv != null && recv.Length > 0)
            {
                var tempBuffer = new byte[receiveBuffer.Length + recv.Length];
                receiveBuffer.CopyTo(tempBuffer, 0);
                recv.CopyTo(tempBuffer, receiveBuffer.Length);
                receiveBuffer = tempBuffer;
            }

            if (receiveBuffer.Length > 0)
            {
                var strRcvd = receiveBuffer;

                var stxLcPos = SearchBytes(strRcvd, STX_LC);

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
                        if (message.StartsWith("PRS")) { 
                            string prefixo = message.Substring(0, 3);
                            string display = message.Substring(3, 3);
                            string value = message.Substring(6, 6);

                            var queueName = Config.AmqpQueueToProduce;

                            Logger.Info($"ReceiveData(): Drive: '{Config.Driver}'. Device: '{Config.Name}'. Message received: {message}");

                            var messageToAmqtp = String.Join(',', Config.AmqpQueueToProduce, Config.Name, $"{Config.PtlId}:{display}", value.TrimStart('0'),DateTime.Now);

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
    }
}
