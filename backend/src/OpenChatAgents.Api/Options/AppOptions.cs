namespace OpenChatAgents.Api.Options;

public class AppOptions
{
    public const string SectionName = "AppSettings";

    public string AppName { get; set; } = "OpenChatAgents";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string LlmModel { get; set; } = "gemma3";
    public string[] CorsOrigins { get; set; } = ["http://localhost:3000"];
    public AwsOptions Aws { get; set; } = new();
}

public class AwsOptions
{
    public string Region { get; set; } = "us-east-1";
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
}
