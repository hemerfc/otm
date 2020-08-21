using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Otm.Server.Device.Ptl
{
    public class TcpClientAdapter : ITcpClientAdapter
    {
        // The real implementation
        private System.Net.Sockets.TcpClient _client;

        public bool Connected => _client?.Connected ?? false;

        public TcpClientAdapter()
        {
        }

        public void Connect(string ip, int port)
        {
            _client = new TcpClient();
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _client.Connect(IPAddress.Parse(ip), port);
        }

        public byte[] GetData()
        {
            try
            {
                if (_client.Available > 0)
                {
                    var buffer = new byte[_client.Available];
                    _client.Client.Receive(buffer);
                    return buffer;
                }
                return null;
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        public int SendData(byte[] buffer)
        {
            if (buffer.Length > 0)
            {
                return _client.Client.Send(buffer);
            }
            return 0;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}