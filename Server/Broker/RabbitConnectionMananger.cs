using System;
using System.Diagnostics;
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
    private static readonly ActivitySource RegisteredActivity  = new ActivitySource("OTM");
    
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
        using (var activity = RegisteredActivity.StartActivity($"CreateConnection : {hostName}"))
        {
            while (true)
            {
                try
                {
                    ConnectionFactory factory = new ConnectionFactory()
                    {
                        HostName = hostName,
                        Port = port
                    };
                    factory.ClientProvidedName = "OTM";
                    Connection = factory.CreateConnection();
                    if (Connection != null && Connection.IsOpen)
                    {
                        break;
                    }
                    else
                    {
                        Connection = null;
                        var waitEvent = new ManualResetEvent(false);
                        waitEvent.WaitOne(1000);
                    }
                }
                catch (Exception e)
                {
                    Connection = null;
                    var waitEvent = new ManualResetEvent(false);
                    waitEvent.WaitOne(1000);
                }
                
            }

        }
    }
    
    public IConnection GetConnection()
    {
        return Connection;
    }
}