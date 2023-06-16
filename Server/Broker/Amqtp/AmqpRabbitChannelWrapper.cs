
using RabbitMQ.Client;
using System.Text;

namespace Otm.Server.Broker.Amqtp
{
    public class AmqpRabbitChannelWrapper : IAmqpChannelWrapper
    {
        private IModel channel;

        private IBasicProperties basicProperties { get; set; }

        public AmqpRabbitChannelWrapper(IModel channel)
        {
            this.channel = channel;
            this.basicProperties = channel.CreateBasicProperties();
            this.basicProperties.Persistent = true;
        }

        public bool IsOpen
        {
            get
            {
                return channel.IsOpen;
            }
        }

        public void Publish(string queueName, string json)
        {
            this.channel.BasicPublish("", queueName, true, basicProperties, Encoding.ASCII.GetBytes(json));
        }
    }
}