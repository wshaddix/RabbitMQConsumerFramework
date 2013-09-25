using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TinyRabbitMQClient;

namespace RabbitMQConsumerFramework
{
    /// <summary>
    /// Manages instantiating queue consumers and hooking them up to their configured queues for
    /// processing
    /// </summary>
    public class QueueConsumerManager
    {
        private readonly string _amqpUri;
        private readonly CancellationTokenSource _cts;
        private readonly ILog _log;
        private readonly List<Task> _tasks;
        private Client _amqpClient;

        public QueueConsumerManager(string amqpUri, ILog log)
        {
            _amqpUri = amqpUri;
            _log = log;
            _cts = new CancellationTokenSource();
            _tasks = new List<Task>();
            ConnectToAmqpServer();
        }

        public void ConsumeQueue<TConsumer, TMessage>(Expression<Func<TConsumer, Func<TMessage, QueueConsumptionResult>>> expression, int numberOfConsumers, bool processErrorQueue = false)
        {
            var messageName = typeof(TMessage).FullName;
            var methodName = GetMethodName(expression);

            // get the name of the exchange
            var exchangeName = GetExchangeName(messageName, processErrorQueue);

            // get the exchange type
            var exchangeType = GetExchangeType(messageName, processErrorQueue);

            // get the queue name
            var queueName = GetQueueName(typeof(TConsumer), messageName, processErrorQueue);

            // get the routingKey
            var routingKey = messageName;

            // for every numberOfConsumers we need to instantiate a TConsumer and start consuming
            // the queue
            for (var i = 0; i < numberOfConsumers; i++)
            {
                // create a new instance of the consuming class
                var consumer = Activator.CreateInstance(typeof(TConsumer));

                // give the consuming class access to our logger so it can send messages back out to
                // the host
                ((QueueConsumerBase)consumer).Logger = _log;

                // perform this on a background Task
                var task = Task.Factory.StartNew(() => _amqpClient.ConsumeQueue<TMessage>(exchangeName, exchangeType, queueName, msg => (QueueConsumptionResult)typeof(TConsumer).InvokeMember(methodName, BindingFlags.Default | BindingFlags.InvokeMethod, null, consumer, new object[] { msg }), _cts.Token, routingKey), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                _log.Debug(m => m("Started Task Id {0} for consuming queue {1}", task.Id, queueName));

                // keep a list of all of the tasks so that we can stop them later
                _tasks.Add(task);
            }
        }

        /// <summary>
        /// Attempts to stop processing queues and disconnects from the amqp server
        /// </summary>
        public void Stop()
        {
            _log.Debug(m => m("Cancelling all background tasks"));
            _cts.Cancel();
            _log.Debug(m => m("Waiting for background tasks to complete. Giving 10 more seconds before termination"));
            Task.WaitAll(_tasks.ToArray(), 10000);
            _log.Debug(m => m("Disconnecting from {0}", _amqpUri));
            _amqpClient.Disconnect();
            _log.Debug(m => m("Disconnected from {0}", _amqpUri));
        }

        private void ConnectToAmqpServer()
        {
            _log.Debug(m => m("Connecting to {0}", _amqpUri));
            _amqpClient = new Client(_amqpUri, _log);
            Globals.RabbitMqClient = _amqpClient;
            _log.Debug(m => m("Connected to {0}", _amqpUri));
        }

        private string GetExchangeName(string messageName, bool processErrorQueue)
        {
            var name = string.Empty;

            if (processErrorQueue)
            {
                return "Errors";
            }

            if (messageName.EndsWith("Command"))
            {
                return "Commands";
            }

            if (messageName.EndsWith("Event"))
            {
                return "Events";
            }

            return name;
        }

        private string GetExchangeType(string messageName, bool processErrorQueue)
        {
            var exchangeType = string.Empty;

            if (processErrorQueue)
            {
                return "direct";
            }
            if (messageName.EndsWith("Command"))
            {
                return "direct";
            }
            if (messageName.EndsWith("Event"))
            {
                return "topic";
            }

            return exchangeType;
        }

        private string GetMethodName<TConsumer, TMessage>(Expression<Func<TConsumer, Func<TMessage, QueueConsumptionResult>>> expression)
        {
            var unaryExpression = expression.Body as UnaryExpression;
            var methodCallExpression = unaryExpression.Operand as MethodCallExpression;
            var constantExpression = methodCallExpression.Object as ConstantExpression;
            var methodInfo = constantExpression.Value as MethodInfo;
            return methodInfo.Name;
        }

        private string GetQueueName(Type consumer, string messageName, bool processErrorQueue)
        {
            // if the message is an event then we need to create a consumer based queue so that
            // multiple consumers can all process the same event (fanout exchange delivers to
            // multiple queues, not multiple consumers so we have to have many queues all bound to
            // the Event exchange)
            if (messageName.EndsWith("Event"))
            {
                return string.Concat("EventConsumer.", consumer.Name, ".", messageName);
            }

            // if it's not an event just name the queue after the message name. If the consumer
            // wants to process the corresponding Error queue then specify the error queue instead
            return processErrorQueue ? string.Concat(messageName, ".Errors") : messageName;
        }
    }
}