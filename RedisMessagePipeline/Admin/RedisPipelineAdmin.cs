using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisMessagePipeline.Admin
{
    /// <summary>
    /// Admin functionality to manage operations on the Redis pipeline, such as starting, stopping, and cleaning.
    /// </summary>
    public class RedisPipelineAdmin : IRedisPipelineAdmin
    {
        private readonly ILogger<RedisPipelineAdmin> logger;
        private readonly RedisPipelineAdminSettings settings;
        private readonly IDistributedLockFactory lockFactory;
        private readonly IDatabase database;

        internal RedisPipelineAdmin(
            ILogger<RedisPipelineAdmin> logger,
            RedisPipelineAdminSettings settings,
            IDistributedLockFactory lockFactory,
            IDatabase database)
        {
            this.logger = logger;
            this.settings = settings;
            this.lockFactory = lockFactory;
            this.database = database;
        }

        /// <summary>
        /// Pushes a new message to the Redis pipeline.
        /// </summary>
        public Task PushAsync(RedisValue redisValue)
        {
            logger.LogDebug("Push a new message '{message}' to '{resource}' redis pipeline", redisValue, settings.Resource);

            RedisKey key = RedisPipelineExtensions.MessagesKey(settings.Resource);
            return database.ListRightPushAsync(key, redisValue);
        }

        /// <summary>
        /// Stops the Redis pipeline.
        /// </summary>
        public Task StopAsync()
        {
            logger.LogDebug("Redis pipeline '{resource}' has been stopped", settings.Resource);

            RedisKey key = RedisPipelineExtensions.StateKey(settings.Resource);
            return database.StringSetAsync(key, RedisPipelineExtensions.STATE_STOPPED);
        }

        /// <summary>
        /// Cleans up resources used by the Redis pipeline.
        /// </summary>
        public async Task CleanAsync(CancellationToken cancellationToken)
        {
            using (IRedLock locker = await lockFactory.CreateLockAsync(
                resource: settings.Resource,
                expiryTime: settings.LockSettings.ExpiryTime,
                waitTime: settings.LockSettings.WaitTime,
                retryTime: settings.LockSettings.RetryTime,
                cancellationToken))
            {
                if (!locker.IsAcquired)
                {
                    logger.LogError("Cannot acquire redlock for Redis pipeline '{resource}'", settings.Resource);
                    throw new InvalidOperationException("Cannot acquire redlock");
                }

                await database.KeyDeleteAsync(new RedisKey[]
                {
                    RedisPipelineExtensions.FailureKey(settings.Resource),
                    RedisPipelineExtensions.StateKey(settings.Resource),
                    RedisPipelineExtensions.MessagesKey(settings.Resource),
                });

                logger.LogDebug("Redis pipeline '{resource}' has been cleaned up", settings.Resource);
            }
        }

        /// <summary>
        /// Resumes operations of the Redis pipeline after a stop.
        /// </summary>
        public async Task ResumeAsync(int skip, CancellationToken cancellationToken)
        {
            using (IRedLock locker = await lockFactory.CreateLockAsync(
                resource: settings.Resource,
                expiryTime: settings.LockSettings.ExpiryTime,
                waitTime: settings.LockSettings.WaitTime,
                retryTime: settings.LockSettings.RetryTime,
                cancellationToken))
            {
                if (!locker.IsAcquired)
                {
                    logger.LogError("Cannot acquire redlock for Redis pipeline '{resource}'", settings.Resource);
                    throw new InvalidOperationException("Unable to acquire redlock");
                }

                RedisValue state = await database.StringGetAsync(RedisPipelineExtensions.StateKey(settings.Resource));
                if (!RedisPipelineExtensions.IsStopped(state))
                {
                    logger.LogError("Cannot resume '{resource}' redis pipeline that has not stopped", settings.Resource);
                    throw new InvalidOperationException("Cannot resume a pipeline that has not stopped");
                }

                ITransaction transaction = database.CreateTransaction();
                RedisKey messagesKey = RedisPipelineExtensions.MessagesKey(settings.Resource);
                RedisKey stateKey = RedisPipelineExtensions.StateKey(settings.Resource);
                Task[] transactionTasks = new Task[] {
                    transaction.ListLeftPopAsync(messagesKey, count: skip),
                    transaction.StringSetAsync(stateKey, 0)
                };
                await transaction.ExecuteAsync();
                await Task.WhenAll(transactionTasks);

                logger.LogDebug("Redis pipeline '{resource}' has been resumed", settings.Resource);
            }
        }
    }
}
