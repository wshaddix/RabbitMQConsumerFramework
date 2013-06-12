namespace RabbitMQConsumerFramework
{
    public class PassthroughLogger : TinyRabbitMQClient.ILogger
    {
        private readonly ILogger _logger;

        public PassthroughLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }

        public void LogInfo(string message, params object[] args)
        {
            _logger.LogInfo(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void LogError(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        public void LogFatal(string message, params object[] args)
        {
            _logger.LogFatal(message, args);
        }
    }
}