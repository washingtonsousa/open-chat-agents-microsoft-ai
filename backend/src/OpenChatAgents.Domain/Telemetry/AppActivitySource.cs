using System.Diagnostics;

namespace OpenChatAgents.Domain.Telemetry;

public static class AppActivitySource
{
    public const string Name = "OpenChatAgents";
    public static readonly ActivitySource Source = new(Name);
}
