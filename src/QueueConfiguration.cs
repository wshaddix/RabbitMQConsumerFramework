namespace RabbitMQConsumerFramework
{
    public class QueueConfiguration
    {
        public int NoOfConsumers { get; set; }
        public string ConsumerClass { get; set; }
        public string ConsumerMethod { get; set; }
        public string QueueName { get; set; }
        public string ExchangeName { get; set; }
        public string ExchangeType { get; set; }
    }
}