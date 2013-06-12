using Newtonsoft.Json;

namespace RabbitMQConsumerFramework
{
    public abstract class QueueConsumerBase
    {
        protected T ConvertMsgToCommand<T>(string msg)
        {
            return JsonConvert.DeserializeObject<T>(msg);
        }

        protected void PublishEvent<T>(T @event)
        {
            Globals.RabbitMqClient.PublishEvent(@event);
        }

        public ILogger Logger { get; set; }
    }
}