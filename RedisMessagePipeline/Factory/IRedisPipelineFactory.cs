using RedisMessagePipeline.Admin;
using RedisMessagePipeline.Consumer;

namespace RedisMessagePipeline.Factory
{
    public interface IRedisPipelineFactory
    {
        RedisPipelineConsumer CreateConsumer(IRedisPipelineHandler handler, RedisPipelineConsumerSettings settings);
        RedisPipelineAdmin CreateAdmin(RedisPipelineAdminSettings settings);
    }
}
