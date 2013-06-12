using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TinyRabbitMQClient;

namespace RabbitMQConsumerFramework
{
    public class ConsumingManager
    {
        private readonly ConsumerConfiguration _configuration;
        private List<Task> _tasks;
        private Client _amqpClient;
        private CancellationTokenSource _cts;

        public ConsumingManager(ConsumerConfiguration configuration)
        {
            _configuration = configuration;
            _cts = new CancellationTokenSource();
            _tasks = new List<Task>();
        }

        public void Start()
        {
            ConnectToAmqpServer();

            StartConsumers();
        }

        private void StartConsumers()
        {
            // for every configured queue we need to start background tasks and let them process
            foreach (var queueConfiguration in _configuration.QueueConfigurations)
            {
                for (int i = 0; i < queueConfiguration.NoOfConsumers; i++)
                {
                    var task = Task.Factory.StartNew(ConsumeQueue(
                        queueConfiguration.ExchangeName,
                        queueConfiguration.ExchangeType,
                        queueConfiguration.QueueName, 
                        queueConfiguration.ConsumerClass, 
                        queueConfiguration.ConsumerMethod), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                   
                   _configuration.Logger.LogDebug("Started Task Id {0} for consuming queue {1}", task.Id, queueConfiguration.QueueName);

                    _tasks.Add(task);
                }
            }
        }

        private Action ConsumeQueue(string exchangeName, string exchangeType, string queueName,  string consumerType, string consumerMethod)
        {
            return () =>
                {
                    // get the type of the consuming class
                    var type = Type.GetType(consumerType);

                    if (null == type)
                    {
                        throw new NullReferenceException(string.Format("Could not load type {0}", consumerType));
                    }

                    // create a new instance of the consuming class
                    var consumer = Activator.CreateInstance(type);
                    
                    // give the consuming class access to our logger so it can send messages back out to the host
                    ((QueueConsumerBase) consumer).Logger = _configuration.Logger;

                    // invoke the specified method on the consuming class
                    _amqpClient.ConsumeQueue(exchangeName, exchangeType, queueName, (msg) => (QueueConsumptionResult)type.InvokeMember(consumerMethod, BindingFlags.Default | BindingFlags.InvokeMethod, null, consumer,new[]{msg} ), _cts.Token);
                };
        }

        public void Stop()
        {
            _configuration.Logger.LogDebug("Stopping");
            _configuration.Logger.LogDebug("Cancelling all background tasks");
            _cts.Cancel();
            _configuration.Logger.LogDebug("Waiting for background tasks to complete. Giving 10 more seconds before termination");
            Task.WaitAll(_tasks.ToArray(), 10000);
            _configuration.Logger.LogDebug("Disconnecting from {0}", _configuration.AmqpConnectionString);
            _amqpClient.Disconnect();
            _configuration.Logger.LogDebug("Disconnected from {0}", _configuration.AmqpConnectionString);
        }

        private void ConnectToAmqpServer()
        {
            _configuration.Logger.LogDebug("Starting Up");
            _configuration.Logger.LogDebug("Connecting to {0}", _configuration.AmqpConnectionString);
            _amqpClient = new Client(_configuration.AmqpConnectionString, new PassthroughLogger(_configuration.Logger));
            Globals.RabbitMqClient = _amqpClient;
            _configuration.Logger.LogDebug("Connected to {0}", _configuration.AmqpConnectionString);
        }
    }
}
