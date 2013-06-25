using Common.Logging;
using Newtonsoft.Json;

namespace RabbitMQConsumerFramework
{
    /// <summary>
    /// Any class that consumes a queue should inherit from the QueueConsumerBase
    /// </summary>
    public abstract class QueueConsumerBase
    {
        /// <summary>
        /// Messages that are dequeued are in json format. This is a helper method that will convert that json string
        /// back into a strong .Net type
        /// </summary>
        /// <typeparam name="T">The .Net type of the message</typeparam>
        /// <param name="msg">The json formatted message that got dequeued</param>
        /// <returns></returns>
        protected T ConvertMsgToCommand<T>(string msg)
        {
            return JsonConvert.DeserializeObject<T>(msg);
        }

        /// <summary>
        /// After a message has been consumed it is likely that you'll want to publish an event so that others
        /// will know that you are finished or take further action (workflows). This is a helper method that 
        /// makes it easy for QueueConsumers to publish events
        /// </summary>
        /// <typeparam name="T">The .Net type of the event to publish</typeparam>
        /// <param name="event">The event that should be published back to the AMQP server</param>
        protected void PublishEvent<T>(T @event)
        {
            Globals.RabbitMqClient.PublishEvent(@event);
        }

        /// <summary>
        /// The Common.Logging ILog that QueueConsumers can use to log messages. This has to be public and not protected
        /// because the ConsumingManager will need access to it when instantiating the QueueConsumer dynamically
        /// </summary>
        public ILog Logger { get; set; }
    }
}