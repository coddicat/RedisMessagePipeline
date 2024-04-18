namespace RedisMessagePipeline.Admin
{
    /// <summary>
    /// Settings for RedisPipelineAdmin to manage Redis pipelines.
    /// </summary>
    public class RedisPipelineAdminSettings
    {
        public RedisPipelineAdminSettings(string resource)
        {
            Resource = resource;
        }
        public string Resource { get; set; }
        public RedisPipelineLockSettings LockSettings { get; set; } = new RedisPipelineLockSettings();
    }
}
