# Redis Message Pipeline

## Overview
This Nuget package provides a robust solution for managing message pipelines with Redis. 
It ensures that each message is handled once and in order, with a focus on high reliability and consistency across distributed systems. 
The package is designed to handle failures gracefully, retrying message handling based on configurable policies or stopping the pipeline until manual intervention.

## Features
- **Single Message Handling**: Each message is processed individually to ensure order and consistency.
- **Failure Handling**: Automatic retries or stops based on user-defined policies to manage message handling failures.
- **Pipeline Control**: Administrative controls to stop, resume, or clean the pipeline, providing flexibility in managing message flow.

## Getting Started
### Installation
Install the package from Nuget:

```bash
dotnet add package RedisMessagePipeline
```

### Configuration
Configure the Redis client and pipeline settings in your application:

```csharp
using Microsoft.Extensions.Logging;
using RedisMessagePipeline;
using RedisMessagePipeline.Admin;
using RedisMessagePipeline.Consumer;
using RedisMessagePipeline.Factory;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379");
RedLockMultiplexer lockMultiplexer = new RedLockMultiplexer(redis);
IDatabase db = redis.GetDatabase();

var loggerFactory = new LoggerFactory();
RedLockFactory lockFactory = RedLockFactory.Create(new List<RedLockMultiplexer> { lockMultiplexer });
RedisPipelineFactory factory = new RedisPipelineFactory(loggerFactory, lockFactory, db);

var consumer = factory.CreateConsumer(new MyMessageHandler(), new RedisPipelineConsumerSettings("my-messages"));
var admin = factory.CreateAdmin(new RedisPipelineAdminSettings("my-messages"));
```

### Usage
Administrate the pipeline:

```csharp
// Stop the pipeline
await admin.StopAsync();

// Push messages
for (int i = 0; i < 10; i++)
{
    await admin.PushAsync($"message:{i}");
}

// Resume the pipeline, skipping problematic messages if necessary
await admin.ResumeAsync(1, CancellationToken.None);
```

Start the consumer to process messages:

```csharp
await consumer.ExecuteAsync(CancellationToken.None);
```

## License
Distributed under the MIT License. See `LICENSE` for more information.

## Support
For support and contributions, please contact the package maintainer at `your_email@example.com`.