using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQConsumerFramework.Configuration;
using TinyRabbitMQClient;

namespace RabbitMQConsumerFramework
{
    /// <summary>
    /// Manages instantiating queue consumers and hooking them up to their configured queues for processing
    /// </summary>
    public class ConsumingManager
    {
        private readonly ConsumerConfiguration _configuration;
        private readonly List<Task> _tasks;
        private Client _amqpClient;
        private readonly CancellationTokenSource _cts;

        public ConsumingManager(ConsumerConfiguration configuration)
        {
            _configuration = configuration;
            _cts = new CancellationTokenSource();
            _tasks = new List<Task>();
        }

        /// <summary>
        /// Starts the process of connecting to the amqp server and consuming queues
        /// </summary>
        public void Start()
        {
            ConnectToAmqpServer();

            StartConsumers();
        }

        private void StartConsumers()
        {
            // for every configured queue we need to start background tasks to process them
            foreach (var queueConfiguration in _configuration.QueueConfigurations)
            {
                for (var i = 0; i < queueConfiguration.NoOfConsumers; i++)
                {
                    // for every consumer we want to start a new background task and instantiate a new instance of the 
                    // consuming class and hook it up to the queue
                    var task = Task.Factory.StartNew(ConsumeQueue(queueConfiguration), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    var configuration = queueConfiguration;
                    _configuration.Log.Debug(m=> m("Started Task Id {0} for consuming queue {1}", task.Id, configuration.QueueName));

                    // keep a list of all of the tasks so that we can stop them later
                    _tasks.Add(task);
                }
            }
        }

        /// <summary>
        /// This method is executed on in the background via a Task for every configured consumer
        /// </summary>
        /// <param name="configuration">The instance of a QueueConfiguration that is currently being processed by the
        /// StartConsumers method</param>
        /// <returns>The method body that gets executed on the background Task</returns>
        private Action ConsumeQueue(QueueConfiguration configuration)
        {
            return () =>
                {
                    // get the type of the consuming class
                    var type = Type.GetType(configuration.ConsumerClass);

                    if (null == type)
                    {
                        throw new NullReferenceException(string.Format("Could not load type {0}", configuration.ConsumerClass));
                    }

                    // create a new instance of the consuming class
                    var consumer = Activator.CreateInstance(type);
                    
                    // give the consuming class access to our logger so it can send messages back out to the host
                    ((QueueConsumerBase) consumer).Logger = _configuration.Log;

                    // invoke the specified method on the consuming class
                    _amqpClient.ConsumeQueue(configuration.ExchangeName, configuration.ExchangeType, configuration.QueueName, msg => (QueueConsumptionResult)type.InvokeMember(configuration.ConsumerMethod, BindingFlags.Default | BindingFlags.InvokeMethod, null, consumer, new object[]{msg} ), _cts.Token);
                };
        }

        /// <summary>
        /// Attempts to stop processing queues and disconnects from the amqp server
        /// </summary>
        public void Stop()
        {
            _configuration.Log.Debug(m=> m("Cancelling all background tasks"));
            _cts.Cancel();
            _configuration.Log.Debug(m=> m("Waiting for background tasks to complete. Giving 10 more seconds before termination"));
            Task.WaitAll(_tasks.ToArray(), 10000);
            _configuration.Log.Debug(m=> m("Disconnecting from {0}", _configuration.AmqpConnectionString));
            _amqpClient.Disconnect();
            _configuration.Log.Debug(m=> m("Disconnected from {0}", _configuration.AmqpConnectionString));
        }

        private void ConnectToAmqpServer()
        {
            _configuration.Log.Debug(m=> m("Connecting to {0}", _configuration.AmqpConnectionString));
            _amqpClient = new Client(_configuration.AmqpConnectionString, _configuration.Log);
            Globals.RabbitMqClient = _amqpClient;
            _configuration.Log.Debug(m=> m("Connected to {0}", _configuration.AmqpConnectionString));
        }
    }
}
