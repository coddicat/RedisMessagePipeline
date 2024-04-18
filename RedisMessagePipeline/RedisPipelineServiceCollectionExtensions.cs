using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisMessagePipeline.Factory;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace RedisMessagePipeline
{
    public static class RedisPipelineServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="RedisPipelineFactory"/> service to the specified <see cref="IServiceCollection"/>,
        /// using a provided <see cref="IConnectionMultiplexer"/> from the service provider for Redis connections.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>        
        /// <returns>The original <see cref="IServiceCollection"/> instance, for chaining further calls.</returns>
        public static IServiceCollection AddRedisPipelineFactory(this IServiceCollection services)
        {
            return services.AddTransient(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                IConnectionMultiplexer multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                return CreateRedisPipelineFactory(loggerFactory, multiplexer);
            });
        }

        /// <summary>
        /// Adds the <see cref="RedisPipelineFactory"/> service to the specified <see cref="IServiceCollection"/>,
        /// creating a new <see cref="IConnectionMultiplexer"/> instance using the provided Redis connection string.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="redisConnectionString">The Redis connection string used to connect to Redis.</param>
        /// <returns>The original <see cref="IServiceCollection"/> instance, for chaining further calls.</returns>
        public static IServiceCollection AddRedisPipelineFactory(this IServiceCollection services, string redisConenctionString)
        {
            return services.AddTransient(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                ConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(redisConenctionString);
                return CreateRedisPipelineFactory(loggerFactory, multiplexer);
            });
        }


        private static IRedisPipelineFactory CreateRedisPipelineFactory(ILoggerFactory loggerFactory, IConnectionMultiplexer multiplexer)
        {
            IDatabase database = multiplexer.GetDatabase();
            RedLockMultiplexer redLockMultiplexer = new RedLockMultiplexer(multiplexer);
            RedLockFactory lockFactory = RedLockFactory.Create(new[] { redLockMultiplexer });

            return new RedisPipelineFactory(loggerFactory, lockFactory, database);
        }
    }
}
