namespace OpenChatAgents.Domain.Models;

public class AgentMcpServer
{
    public Guid AgentId { get; set; }
    public Guid McpServerId { get; set; }

    public Agent? Agent { get; set; }
    public McpServer? McpServer { get; set; }
}
