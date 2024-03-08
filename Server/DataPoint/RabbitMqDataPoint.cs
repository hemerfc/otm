
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Otm.Server.ContextConfig;
using System.Data.SqlClient;
using System.Text;
using Jint;
using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;

namespace Otm.Server.DataPoint
{
    public class RabbitMqDataPoint : IDataPoint
    {
        public string Name { get { return Config.Name; } }
        private DataPointConfig Config { get; set; }
        //public Engine Engine { get;  }
        public bool DebugMessages { get; set; }
        public string Driver { get; }
        public string Script { get; }
        public string CronExpression { get; }
        
        private readonly ILogger logger;

        #region Rabbit
        private IConnection RabbitConnection { get; set; }
        private IModel RabbitChannel { get; set; }
        private IBasicProperties BasicProperties { get; set; }
        private string QueueName { get; set; }

        private (IConnection connection, IModel channel, IBasicProperties basicProperties) CreateConnectionAndChannel(Dictionary<string, string> configDict)
        {
            try
            {
                var hostname = configDict["hostname"];
                var port = configDict["port"];
                var user = configDict["user"];
                var password = configDict["password"];
                var queueName = configDict["queue"];
                
                var factory = new ConnectionFactory
                {
                    HostName = hostname, 
                    Port = int.Parse(port),
                    UserName = user,
                    Password = password
                };
                var connection = factory.CreateConnection();
                var channel = connection.CreateModel();
                var basicProperties = channel.CreateBasicProperties();
                basicProperties.Persistent = true;
                
                channel.QueueDeclare(queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                
              
                return (connection,channel, basicProperties);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"RabbitMqDevice|GetDeviceParameter|Device: {Config.Name}| {ex}");
                return (null, null, null);
            }
        }
        
        // this function return a Dictionary from string split by ';' and than split by '='
        //  first part before '=' is the key and the second part is the value
        private Dictionary<string, string> GetConfigDict(string config)
        {
            return config.Split(';').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1].Trim());
        }

        #endregion

        public RabbitMqDataPoint(DataPointConfig config, ILogger logger)
        {
            this.ConfigDict = GetConfigDict(config.Config);
            this.logger = logger;
            this.Config = config;
            // this.Engine = new Engine();
            this.DebugMessages = config.DebugMessages;
            this.Driver = config.Driver;
            this.Script = config.Script;
            this.QueueName = ConfigDict["queue"];
            
            (RabbitConnection,RabbitChannel,BasicProperties) = CreateConnectionAndChannel(ConfigDict);
        }

        public Dictionary<string, string> ConfigDict { get; set; }


        public DataPointParamConfig GetParamConfig(string name)
        {
            return Config.Params.FirstOrDefault(x => x.Name == name);
        }

        public IDictionary<string, object> Execute(IDictionary<string, object> input)
        {
            // foreach (var param in Config.Params)
            // {
            //     if (param.Mode == Modes.FromOTM)
            //         Engine.SetValue(param.Name, input[param.Name]);
            // }

            // Engine.Execute(Config.Script);

            // is not connected, try to connect
            if(RabbitChannel == null)
                (RabbitConnection,RabbitChannel,BasicProperties) = CreateConnectionAndChannel(ConfigDict);
            
            if(RabbitChannel != null)
            {
                input.Add("DATETIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                
                var messageStr = FormatFromDictionary(Script, input.ToDictionary(x => x.Key, x => x.Value.ToString()));
                var json = JsonConvert.SerializeObject(new { Body = messageStr });
                
                RabbitChannel.BasicPublish("", QueueName, true, BasicProperties, Encoding.ASCII.GetBytes(json));
                var log = $"Publish to {QueueName} Message: {messageStr}";
                
                logger.Log(LogLevel.Info, log);
            }
            var output = new Dictionary<string, object>();
            // foreach (var param in Config.Params)
            // {
            //     if (param.Mode == Modes.ToOTM)
            //         output[param.Name] = Convert.ChangeType(Engine.GetValue(param.Name).ToObject(), param.TypeCode);
            // }
            return output;
        }
        
        // this function format a string using a dictionary of values
        private static string FormatFromDictionary(string formatString, Dictionary<string, string> valueDict) 
        {
            int i = 0;
            StringBuilder newFormatString = new StringBuilder(formatString);
            Dictionary<string, int> keyToInt = new Dictionary<string,int>();
            foreach (var tuple in valueDict)
            {
                newFormatString = newFormatString.Replace("{" + tuple.Key + "}", "{" + i.ToString() + "}");
                keyToInt.Add(tuple.Key, i);
                i++;                    
            }
            return String.Format(newFormatString.ToString(), valueDict.OrderBy(x => keyToInt[x.Key]).Select(x => x.Value).ToArray());
        }

        public bool CheckConnection()
        {
            throw new NotImplementedException();
        }

        public bool CheckFunction()
        {
            throw new NotImplementedException();
        }

        public List<DataPointFunction> GetFunctions()
        {
            throw new NotImplementedException();
        }
    }
}