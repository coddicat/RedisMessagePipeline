using RedisMessagePipeline.Admin;
using RedisMessagePipeline.Consumer;

namespace RedisMessagePipeline.Factory
{
    public interface IRedisPipelineFactory
    {
        IRedisPipelineConsumer CreateConsumer(IRedisPipelineHandler handler, RedisPipelineConsumerSettings settings);
        IRedisPipelineAdmin CreateAdmin(RedisPipelineAdminSettings settings);
    }
}
