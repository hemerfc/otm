using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Otm.Server.Device.Ptl
{
    public interface ITcpClientAdapter : IDisposable
    {
        byte[] GetData();
        int SendData(byte[] buffer);
        void Connect(string ip, int port);
        bool Connected { get; }

        //bool Connected();
    }
}