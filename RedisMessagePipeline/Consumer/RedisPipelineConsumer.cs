using Microsoft.Extensions.Logging;
using RedLockNet;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedisMessagePipeline.Consumer
{
    /// <summary>
    /// Consumes messages from a Redis pipeline and processes them according to the specified handler logic.
    /// </summary>
    public class RedisPipelineConsumer : IRedisPipelineConsumer
    {
        private readonly ILogger<RedisPipelineConsumer> logger;
        private readonly IRedisPipelineHandler handler;
        private readonly IDistributedLockFactory lockFactory;
        private readonly IDatabase database;
        private readonly RedisPipelineConsumerSettings settings;

        internal RedisPipelineConsumer(
            ILogger<RedisPipelineConsumer> logger,
            IRedisPipelineHandler handler,
            RedisPipelineConsumerSettings settings,
            IDistributedLockFactory lockFactory,
            IDatabase database)
        {
            this.logger = logger;
            this.handler = handler;
            this.lockFactory = lockFactory;
            this.database = database;
            this.settings = settings;
        }

        /// <summary>
        /// Executes the consumer processing, continually polling for and handling new messages.
        /// </summary>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("RedisPipelineConsumer '{resource}' has been executed.", settings.Resource);

            while (!cancellationToken.IsCancellationRequested)
            {
                bool success = await PollAsync(cancellationToken);
                if (!success)
                {
                    await Task.Delay(settings.PullInterval, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Polls for new messages, processes them, and handles any resulting state changes.
        /// </summary>
        private async Task<bool> PollAsync(CancellationToken cancellationToken)
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
                    return false;
                }

                RedisValue state = await database.StringGetAsync(RedisPipelineExtensions.StateKey(settings.Resource));
                if (RedisPipelineExtensions.IsStopped(state))
                {
                    return false;
                }

                RedisValue message = await database.ListLeftPopAsync(RedisPipelineExtensions.MessagesKey(settings.Resource));
                if (message.IsNull)
                {
                    return false;
                }

                bool success = await HandleMessageAsync(message, cancellationToken);

                if (success)
                {
                    await HandleSuccessAsync();
                    return true;
                }

                await HandleFailureAsync(message, state);
                return false;
            }
        }

        /// <summary>
        /// Attempts to process a single message and handle its result.
        /// </summary>
        private async Task<bool> HandleMessageAsync(RedisValue message, CancellationToken cancellationToken)
        {
            bool success = false;
            try
            {
                success = await handler.HandleAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Handle message '{message}' from redis pipeline '{resource}' has been failed.", message, settings.Resource);
                await StoreFailureAsync(message, ex);
            }

            return success;
        }

        /// <summary>
        /// Records a failure in processing to the Redis failure log.
        /// </summary>
        private async Task StoreFailureAsync(RedisValue message, Exception ex)
        {
            RedisPipelineFailure failure = new RedisPipelineFailure
            {
                Exception = ex.Message,
                Message = message,
                Timestamp = DateTime.UtcNow.Ticks
            };
            await database.StringSetAsync(RedisPipelineExtensions.FailureKey(settings.Resource), JsonSerializer.Serialize(failure));
        }

        /// <summary>
        /// Handles successful message processing by resetting the pipeline state and clearing failures.
        /// </summary>
        private async Task HandleSuccessAsync()
        {
            ITransaction transaction = database.CreateTransaction();
            Task[] transactionTasks = new Task[] {
                transaction.StringSetAsync(RedisPipelineExtensions.StateKey(settings.Resource), 0),
                transaction.KeyDeleteAsync(RedisPipelineExtensions.FailureKey(settings.Resource))
            };
            await transaction.ExecuteAsync();
            await Task.WhenAll(transactionTasks);
        }

        /// <summary>
        /// Handles message processing failures by retrying or stopping the pipeline based on the retry policy.
        /// </summary>
        private async Task HandleFailureAsync(RedisValue message, RedisValue state)
        {
            logger.LogWarning("Handle message '{message}' from redis pipeline '{resource}' has been failed.", message, settings.Resource);

            await database.ListLeftPushAsync(RedisPipelineExtensions.MessagesKey(settings.Resource), message);
            RedisKey stateKey = RedisPipelineExtensions.StateKey(settings.Resource);
            if (state.IsNull)
            {
                await database.StringSetAsync(stateKey, 1);
            }
            else if (int.TryParse(state, out int count))
            {
                count++;
                bool shouldStop = count >= settings.MaxRetries;
                await database.StringSetAsync(stateKey, shouldStop ? RedisPipelineExtensions.STATE_STOPPED : $"{count}");

                if (shouldStop)
                {
                    logger.LogWarning("Redis pipeline '{resource}' has been stopped.", settings.Resource);
                }
            }
        }
    }
}
