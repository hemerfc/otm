using System;
using System.Text;

namespace Otm.Server.Device.Palantir.Models
{
    public struct MessageKeepAlive
    {
        public MessageKeepAlive(MessageCodesKeepAlive sequence = MessageCodesKeepAlive.ClientSend)
        {
            Code = string.Empty;
            RequestDateTime = DateTime.Now;

            switch (sequence)
            {
                case MessageCodesKeepAlive.ClientSend:
                    Code = "K01";
                    break;
                case MessageCodesKeepAlive.ServerResposne:
                    Code = "K02";
                    break;
                default:
                    break;
            }
        }

        public string Code { get; set; }
        public DateTime RequestDateTime { get; set; }

        public byte[] GetCompleteMessage() => Encoding.ASCII.GetBytes($"{MessageConstants.STX}{Code},{RequestDateTime.ToString(MessageConstants.DATETIME_PATTERN)}{MessageConstants.ETX}");
    }
}
