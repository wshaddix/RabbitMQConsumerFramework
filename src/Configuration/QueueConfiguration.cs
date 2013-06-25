namespace RabbitMQConsumerFramework.Configuration
{
    /// <summary>
    /// Every queue that gets processed will be represented by a QueueConfiguration for the ConsumingManager to know what to do
    /// </summary>
    public class QueueConfiguration
    {
        /// <summary>
        /// The number of consumers (threads) that you want processing the queue. If your queue is being published to faster
        /// than you can consume it and you have cpu/memory/network resources available you can increase this value and 
        /// process messages in a round-robin fashion amongst the consumers
        /// </summary>
        public int NoOfConsumers { get; set; }

        /// <summary>
        /// The fully qualified typename of the class that you want to process the specified queue with. A new instance
        /// of this class will be dynamically instantiated based on the NoOfConsumers value.
        /// </summary>
        public string ConsumerClass { get; set; }

        /// <summary>
        /// The name of the method on the ConsumerClass that you would like to execute when a new message arrives in 
        /// the message queue that you are processing. The json message will be passed to this method and it should 
        /// return a TinyRabbitMQClient.QueueConsumptionResult
        /// </summary>
        public string ConsumerMethod { get; set; }

        /// <summary>
        /// The name of the queue that you want to process
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// The name of the exchange that should be bound to the queue that you want to process. This is needed to 
        /// ensure that messages don't get black-holed if one gets published before anyone is listening
        /// </summary>
        public string ExchangeName { get; set; }

        /// <summary>
        /// The type of exchange that should be bound to the queue that you want to process. This is needed to 
        /// ensure that messages don't get black-holed if one gets published before anyone is listening
        /// </summary>
        public string ExchangeType { get; set; }
    }
}