using Microsoft.Extensions.Logging;
using RedisMessagePipeline.Admin;
using RedisMessagePipeline.Consumer;
using RedLockNet;
using StackExchange.Redis;

namespace RedisMessagePipeline.Factory
{
    /// <summary>
    /// Factory for creating Redis pipeline consumers and admins with configured dependencies.
    /// </summary>
    public class RedisPipelineFactory : IRedisPipelineFactory
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly IDistributedLockFactory lockFactory;
        private readonly IDatabase database;

        public RedisPipelineFactory(ILoggerFactory loggerFactory, IDistributedLockFactory lockFactory, IDatabase database)
        {
            this.loggerFactory = loggerFactory;
            this.lockFactory = lockFactory;
            this.database = database;
        }

        /// <summary>
        /// Creates a RedisPipelineConsumer with specific handler and settings.
        /// </summary>
        public IRedisPipelineConsumer CreateConsumer(IRedisPipelineHandler handler, RedisPipelineConsumerSettings settings)
        {
            return new RedisPipelineConsumer(loggerFactory.CreateLogger<RedisPipelineConsumer>(), handler, settings, lockFactory, database);
        }

        /// <summary>
        /// Creates a RedisPipelineAdmin with specific settings.
        /// </summary>
        public IRedisPipelineAdmin CreateAdmin(RedisPipelineAdminSettings settings)
        {
            return new RedisPipelineAdmin(loggerFactory.CreateLogger<RedisPipelineAdmin>(), settings, lockFactory, database);
        }
    }
}
