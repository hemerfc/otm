using System.Net;
using System.Net.Sockets;

namespace Otm.StressTest;

public class BaseTcpServer
{
    private TcpListener _listener;
    private bool _listen = true;
    private int _clientCount = 0;
    private static async Task HandleClient(TcpClient clt)
    {        
        Console.WriteLine("Client connected");
        
        using NetworkStream ns = clt.GetStream();
        using StreamReader sr = new StreamReader(ns);
        using StreamWriter sw = new StreamWriter(ns){ AutoFlush = true };
        
        while (true)
        {
            if (clt.Connected)
            {
                var str = await sr.ReadToEndAsync();
                if (!string.IsNullOrEmpty(str))
                {
                    // Process data
                    await sw.WriteAsync(" res:" + str);
                }
                else
                {
                    // no data
                    await Task.Delay(100);
                }
            }
            else
            {
                break;
            }
        }
    }
    
    public BaseTcpServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }
    
    public async void Start()
    {
        _listener.Start();
        
        while (_listen)
            if (_listener.Pending())
                await HandleClient(await _listener.AcceptTcpClientAsync());
            else
                await Task.Delay(100); //<--- timeout
    }
}