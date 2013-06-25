using TinyRabbitMQClient;

namespace RabbitMQConsumerFramework
{
    /// <summary>
    /// Holds references to singleton classes that are needed in multiple places throughout this
    /// library
    /// </summary>
    internal static class Globals
    {
        internal static Client RabbitMqClient { get; set; }
    }
}