using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;

namespace RedisMessagePipeline
{
    /// <summary>
    /// Defines an interface for handling messages from a Redis pipeline.
    /// </summary>
    public interface IRedisPipelineHandler
    {
        /// <summary>
        /// Process a message from Redis pipeline and determine if the handling was successful.
        /// </summary>
        /// <param name="redisValue">The message to handle.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation, containing a boolean result.</returns>
        Task<bool> HandleAsync(RedisValue redisValue, CancellationToken cancellationToken);
    }
}
