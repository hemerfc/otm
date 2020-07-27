using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Otm.Server.Device.Ptl
{
    public class TcpClientAdapter : ITcpClientAdapter
    {
        // The real implementation
        private readonly System.Net.Sockets.TcpClient _client;

        public bool Connected => _client.Connected;

        public TcpClientAdapter()
        {
            _client = new System.Net.Sockets.TcpClient();
        }

        public void Connect(string ip, int port)
        {
            _client.Connect(IPAddress.Parse(ip), port);
        }

        public byte[] GetData()
        {
            if (_client.Available > 0)
            {
                var buffer = new byte[_client.Available - 1];
                _client.Client.Receive(buffer);
                return buffer;
            }
            return null;
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