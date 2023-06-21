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

        public bool Connected => _client? .Connected ?? false;

        public TcpClientAdapter()
        {
        }

        public void Connect(string hostName, int port)
        {
            _client = new TcpClient();
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            try
            {
                _client.ConnectAsync(hostName, port).Wait(1000);
            }
            catch (SocketException ex)
            {
                throw new Exception($"Erro de conex�o: {ex.Message}");
            }

            //// resolve o nome pelo DNS
            //var hostEntry = Dns.GetHostEntry(hostName);
            //if (hostEntry.AddressList.Length > 0)
            //{
            //    _client.Connect(hostEntry.AddressList[0], port);
            //}
            //else
            //{
            //    throw new Exception($"HostName invalido {hostName} ou n�o foi possivel resolver para um IP valido!");
            //}
        }

        //public bool Connected()
        //{
        //    if (_client == null)
        //    {
        //        return false;
        //    }

        //    if (!_client.Client.Connected)
        //    {
        //        return false;
        //    }

        //    //try
        //    //{
        //    //    _client.Client.Send(new byte[0]);
        //    //}
        //    //catch (SocketException)
        //    //{
        //    //    return false;
        //    //}

        //    return true;
        //}


        public byte[] GetData()
        {
            if (_client.Available > 0)
            {
                var buffer = new byte[_client.Available];
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