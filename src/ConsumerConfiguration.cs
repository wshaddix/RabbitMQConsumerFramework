using System.Collections.Generic;

namespace RabbitMQConsumerFramework
{
    public class ConsumerConfiguration
    {
        public List<QueueConfiguration> QueueConfigurations { get; set; }
        public string AmqpConnectionString { get; set; }
        public ILogger Logger { get; set; }

        public ConsumerConfiguration()
        {
            QueueConfigurations = new List<QueueConfiguration>();
        }
    }
}