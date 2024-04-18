namespace RedisMessagePipeline
{
    /// <summary>
    /// Represents a failure within the Redis pipeline, capturing exception details and timestamps.
    /// </summary>
    public class RedisPipelineFailure
    {
        public string Exception { get; set; }
        public string Message { get; set; }
        public long Timestamp { get; set; }
    }
}
