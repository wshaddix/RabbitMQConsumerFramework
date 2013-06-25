using Common.Logging;
using System.Collections.Generic;

namespace RabbitMQConsumerFramework.Configuration
{
    /// <summary>
    /// Hold the configuration for the ConsumingManager to use to process message queues
    /// </summary>
    public class ConsumerConfiguration
    {
        /// <summary>
        /// Every queue that gets processed has a QueueConfiguration that tells the ConsumingManager what to do
        /// </summary>
        public List<QueueConfiguration> QueueConfigurations { get; set; }

        /// <summary>
        /// The AMQP Uri. This should conform to the AMQP URI Specification - http://www.rabbitmq.com/uri-spec.html
        /// </summary>
        public string AmqpConnectionString { get; set; }

        /// <summary>
        /// All of the queue consumer instances will get a reference to this Common.Logging.ILog so that 
        /// log messages can be consumed. Also used by the ConsumingManager to log messages
        /// </summary>
        public ILog Log { get; set; }

        public ConsumerConfiguration()
        {
            QueueConfigurations = new List<QueueConfiguration>();
        }
    }
}