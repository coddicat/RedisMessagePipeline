using System;

namespace RedisMessagePipeline.Consumer
{
    /// <summary>
    /// Configuration settings for RedisPipelineConsumer, including resource identifiers and retry logic.
    /// </summary>
    public class RedisPipelineConsumerSettings
    {
        public RedisPipelineConsumerSettings(string resource)
        {
            Resource = resource;
        }
        public string Resource { get; set; }
        public int MaxRetries { get; set; } = int.MaxValue;

        /// <summary>
        /// Interval between unsuccessful handling or empty fetching attempts.
        /// </summary>
        public TimeSpan PullInterval { get; set; } = TimeSpan.FromMilliseconds(500);
        public RedisPipelineLockSettings LockSettings { get; set; } = new RedisPipelineLockSettings();
    }
}
