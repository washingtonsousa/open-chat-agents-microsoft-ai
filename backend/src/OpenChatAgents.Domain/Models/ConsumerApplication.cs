namespace OpenChatAgents.Domain.Models;

public class ConsumerApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecretHash { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? CreatedByUser { get; set; }
}
