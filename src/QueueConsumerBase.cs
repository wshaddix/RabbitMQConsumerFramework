using Common.Logging;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using TinyRabbitMQClient;

namespace RabbitMQConsumerFramework
{
    /// <summary>
    /// Any class that consumes a queue should inherit from the QueueConsumerBase
    /// </summary>
    public abstract class QueueConsumerBase
    {
        /// <summary>
        /// The Common.Logging ILog that QueueConsumers can use to log messages. This has to be
        /// public and not protected because the QueueConsumerManager will need access to it when
        /// instantiating the QueueConsumer dynamically
        /// </summary>
        public ILog Logger { get; set; }

        /// <summary>
        /// Messages that are dequeued are in json format. This is a helper method that will convert
        /// that json string back into a strong .Net type
        /// </summary>
        /// <typeparam name="T">The .Net type of the message</typeparam>
        /// <param name="msg">The json formatted message that got dequeued</param>
        /// <returns></returns>
        protected T ConvertMsgToCommand<T>(string msg)
        {
            return JsonConvert.DeserializeObject<T>(msg);
        }

        protected QueueConsumptionResult Do(Action consumingAction, [CallerMemberName] string methodName = "")
        {
            try
            {
                // perform the work that we want to do
                consumingAction();

                // notify our host
                Logger.Debug(m => m(string.Format("Thread {0} :: Processed {1}", Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture), methodName)));

                // return success
                return new QueueConsumptionResult { WasSuccessful = true };
            }
            catch (Exception ex)
            {
                // log the error with whatever Common.Logging sink the consumer is using
                Logger.Error(m => m(ex.Message));

                // return a failure with the reason
                return new QueueConsumptionResult { WasSuccessful = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// After a message has been consumed it is likely that you'll want to publish an event so
        /// that others will know that you are finished or take further action (workflows). This is
        /// a helper method that makes it easy for QueueConsumers to publish events
        /// </summary>
        /// <typeparam name="T">The .Net type of the event to publish</typeparam>
        /// <param name="event">The event that should be published back to the AMQP server</param>
        protected void PublishEvent<T>(T @event)
        {
            Globals.RabbitMqClient.PublishEvent(@event);
        }

        protected void QueueCommand<T>(T cmd)
        {
            Globals.RabbitMqClient.QueueCommand(cmd);
        }
    }
}