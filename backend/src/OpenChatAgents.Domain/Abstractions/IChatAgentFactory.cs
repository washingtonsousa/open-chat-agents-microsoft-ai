using Microsoft.Extensions.AI;
using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Domain.Abstractions;

public interface IChatAgentFactory
{
    IAsyncEnumerable<string> StreamAsync(Agent agent, IEnumerable<ChatMessage> messages);
}
