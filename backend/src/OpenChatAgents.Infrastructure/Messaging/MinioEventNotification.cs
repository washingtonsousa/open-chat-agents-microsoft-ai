using System.Text.Json.Serialization;

namespace OpenChatAgents.Infrastructure.Messaging;

public class MinioEventNotification
{
    [JsonPropertyName("Records")]
    public List<MinioEventRecord> Records { get; set; } = [];
}

public class MinioEventRecord
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("s3")]
    public MinioEventS3 S3 { get; set; } = new();
}

public class MinioEventS3
{
    [JsonPropertyName("bucket")]
    public MinioEventBucket Bucket { get; set; } = new();

    [JsonPropertyName("object")]
    public MinioEventObject Object { get; set; } = new();
}

public class MinioEventBucket
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class MinioEventObject
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}
