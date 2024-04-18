using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;

namespace RedisMessagePipeline.Admin
{
    public interface IRedisPipelineAdmin
    {
        Task PushAsync(RedisValue redisValue);
        Task StopAsync();
        Task CleanAsync(CancellationToken cancellationToken);
        Task ResumeAsync(int skip, CancellationToken cancellationToken);
    }
}
