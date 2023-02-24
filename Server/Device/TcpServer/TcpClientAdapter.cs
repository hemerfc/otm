using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Otm.Server.Device.Ptl;

namespace Otm.Server.Device.TcpServer
{
    public class TcpClientAdapter : ITcpClientAdapter
    {
        // The real implementation
        private TcpClient _client;

        public bool Connected => _client?.Connected ?? false;

        public TcpClientAdapter()
        {
        }

        public void Connect(string hostName, int port)
        {
            _client = new TcpClient();
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            try
            {
                _client.Connect(hostName, port);
            }
            catch (SocketException ex)
            {
                throw new Exception($"Erro de conexão: {ex.Message}");
            }

            //// resolve o nome pelo DNS
            //var hostEntry = Dns.GetHostEntry(hostName);
            //if (hostEntry.AddressList.Length > 0)
            //{
            //    _client.Connect(hostEntry.AddressList[0], port);
            //}
            //else
            //{
            //    throw new Exception($"HostName invalido {hostName} ou não foi possivel resolver para um IP valido!");
            //}
        }

        public byte[] GetData()
        {
            //try
            //{
            if (_client.Available > 0)
            {
                var buffer = new byte[_client.Available];
                _client.Client.Receive(buffer);
                return buffer;
            }
            return null;
            //}
            //catch (Exception ex) {
            //    throw ex;
            //}
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