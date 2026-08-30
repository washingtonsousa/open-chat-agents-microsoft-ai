using System.Diagnostics;

namespace OpenChatAgents.Infrastructure.Telemetry;

/// <summary>
/// Shared <see cref="ActivitySource"/> for manual spans that tie together the business pipeline
/// (moderation, retrieval, ingestion) around the auto-instrumented agent/model calls.
/// </summary>
public static class AppActivitySource
{
    public const string Name = "OpenChatAgents";

    public static readonly ActivitySource Source = new(Name);
}
