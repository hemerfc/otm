using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otm.Server.Device.Ptl
{
    public class SmartPickingPtlDevice : IPtlDevice
    {

        public override void ApagarDisplays(IEnumerable<PtlBaseClass> listaApagar)
        {
            foreach (var itemApagar in listaApagar.ToList())
            {
                string displayId = itemApagar.GetDisplayId().ToString().PadLeft(3, '0');

                string mesage = $"CLR{displayId}|";
                var buffer = System.Text.Encoding.ASCII.GetBytes(mesage);

                sendDataQueue.Enqueue(buffer.ToArray());
                ListaLigados.Remove(itemApagar);
            }


        }

        public override void AcenderDisplays(IEnumerable<PtlBaseClass> listaAcender)
        {
            foreach (var itemAcender in listaAcender.ToList())
            {
                string displayId = itemAcender.GetDisplayId().ToString().PadLeft(3, '0');
                string displayCode = "";
                string mesage = "";

                if (itemAcender.IsMasterMessage)
                {
                    displayCode = itemAcender.DisplayValue.PadLeft(6, '0');
                    mesage = $"SHW{displayId}{displayCode}|";
                }
                else
                {
                    displayCode = itemAcender.DisplayValue.PadLeft(3,'0');
                    var color = itemAcender.DisplayColor;
                    mesage = $"SHW{displayId}{displayCode}26|";

                    if (itemAcender.IsBlinking)
                    {
                        //Implementar
                    }

                }

                var buffer = System.Text.Encoding.ASCII.GetBytes(mesage);

                sendDataQueue.Enqueue(buffer.ToArray());

                ListaLigados.Add(itemAcender);
            }
        }

        public override bool Received(byte[] recv)
        {
            var received = false;
            var message = "";
            if (recv != null && recv.Length > 0)
            {
                message = ASCIIEncoding.ASCII.GetString(recv);
                Logger.Info($"ReceiveData(): Hardware:SmartPiking Drive: '{Config.Driver}'. Device: '{Config.Name}'. Received: '{recv}'.\tString: '{message}'\t ByteArray: '{string.Join(", ", recv)}'");
                
                
                if (recv.Length == 12) { 
                    string telegrama = message.Substring(0, 3);
                    string displayCode = message.Substring(3, 3);
                    string info = message.Substring(6, 6);

                    // ptl01|AT|001|000001
                    var sendCMD = $"{Config.Name}|AT|{displayCode}|{info}";
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
            }

            

            return received;
        }
    }
}
