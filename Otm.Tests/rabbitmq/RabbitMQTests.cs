using System;
using Xunit;
using Otm.Config;
using Otm.Device;
using Snap7;
using Moq;
using Otm.Logger;
using NLog;
using MQTTnet;
using MQTTnet.Client.Options;
//using MQTTnet.
using MQTTnet.Extensions.ManagedClient;

/* 
namespace Otm.Test.RabbitMQ
{
    public class RabbitMQTests
    {
        [Fact]
        public void SendMessage()
        {
            
            Assert.Equal(2, tag2);
        }       
        
        [Fact]
        public void ReceiveMessage()
        {
            
            Assert.Equal(2, tag2);
        }



        public async void CreateMqttClient()
        {
            // Setup and start a managed MQTT client.
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId("Client1")
                    .WithTcpServer("broker.hivemq.com")
                    .WithTls()
                    .Build())
                .Build();

            var mqttClient = new MqttFactory().CreateManagedMqttClient();
            await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("my/topic").Build());
            await mqttClient.StartAsync(options);

            // StartAsync returns immediately, as it starts a new thread using Task.Run, 
            // and so the calling thread needs to wait.            
        }
    }
}
*/