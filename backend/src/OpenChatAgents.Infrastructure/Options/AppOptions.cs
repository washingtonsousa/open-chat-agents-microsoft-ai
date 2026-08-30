namespace OpenChatAgents.Infrastructure.Options;

public class AppOptions
{
    public const string SectionName = "AppSettings";

    public string AppName { get; set; } = "OpenChatAgents";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string LlmModel { get; set; } = "gemma3";
    public string[] CorsOrigins { get; set; } = ["http://localhost:3000"];
    public AwsOptions Aws { get; set; } = new();
    public JwtOptions Jwt { get; set; } = new();
    public MinioOptions Minio { get; set; } = new();
    public RabbitMqOptions RabbitMq { get; set; } = new();
    public WeaviateOptions Weaviate { get; set; } = new();
    public Argon2Options Argon2 { get; set; } = new();
    public TelemetryOptions Telemetry { get; set; } = new();
}

public class AwsOptions
{
    public string Region { get; set; } = "us-east-1";
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
}

public class JwtOptions
{
    public string Secret { get; set; } = "change-me-in-production-please-use-a-long-random-secret";
    public string Issuer { get; set; } = "OpenChatAgents";
    public string Audience { get; set; } = "OpenChatAgents";
    public int ExpiryMinutes { get; set; } = 480;
}

public class MinioOptions
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Bucket { get; set; } = "kb-documents";
    public bool UseSsl { get; set; }
}

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ExchangeName { get; set; } = "minio-events";
    public string QueueName { get; set; } = "kb-ingestion";
}

public class WeaviateOptions
{
    public string RestEndpoint { get; set; } = "localhost";
    public int RestPort { get; set; } = 8085;
    public string GrpcEndpoint { get; set; } = "localhost";
    public int GrpcPort { get; set; } = 50051;
    public bool UseSsl { get; set; }
}

public class Argon2Options
{
    public int MemoryKb { get; set; } = 19_456;
    public int Iterations { get; set; } = 2;
    public int Parallelism { get; set; } = Environment.ProcessorCount;
}

public class TelemetryOptions
{
    public bool Enabled { get; set; } = true;
    public string OtlpEndpoint { get; set; } = "http://localhost:3001/api/public/otel";
    public string? LangfusePublicKey { get; set; }
    public string? LangfuseSecretKey { get; set; }

    /// <summary>
    /// When true, raw prompt/response content and function-call arguments are included in traces.
    /// Off by default because this is sensitive user data — only enable for a trusted, secured
    /// Langfuse instance.
    /// </summary>
    public bool CaptureSensitiveContent { get; set; }
}
