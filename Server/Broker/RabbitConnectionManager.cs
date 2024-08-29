using System.Threading;
using RabbitMQ.Client;

namespace Otm.Server.Broker;

public sealed class RabbitConnectionManager
{
    private static RabbitConnectionManager instance = null;
    private static readonly object createlock = new object();
    private static readonly object connectionLock = new object();
    private static  IConnection Connection { get; set; }
    private static string HostName { get; set; }
    private static int Port { get; set; }
    
    RabbitConnectionManager(string hostName, int port)
    {
        HostName = hostName;
        Port = port;
        CreateConnection(hostName, port);
    }

    public static RabbitConnectionManager GetInstance(string hostName, int port)
    {
        lock (createlock)
        {
            if (instance == null)
            {
                instance = new RabbitConnectionManager(hostName, port);
            }
            return instance;
        }
    }
    
    private void CreateConnection(string hostName, int port)
    {            
        ConnectionFactory factory = new ConnectionFactory()
        {
            HostName = hostName,
            Port = port
        };
        Connection = factory.CreateConnection();
    }
    
    public IConnection GetConnection()
    {
        if(Connection.IsOpen)
            return Connection;
        
        lock (connectionLock)
        {
            if (!Connection.IsOpen)
            {
                CreateConnection(HostName, Port);
                
                // wait 100ms
                var waitEvent = new ManualResetEvent(false);
                waitEvent.WaitOne(100);
            }
            return Connection;
        }
    }
}