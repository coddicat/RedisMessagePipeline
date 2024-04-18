using System.Threading;
using System.Threading.Tasks;

namespace RedisMessagePipeline.Consumer
{
    public interface IRedisPipelineConsumer
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
