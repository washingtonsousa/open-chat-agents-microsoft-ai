using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Domain.Models;

namespace OpenChatAgents.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<User> Users => Set<User>();
    public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();
    public DbSet<KbDocument> KbDocuments => Set<KbDocument>();
    public DbSet<KbChunkRef> KbChunkRefs => Set<KbChunkRef>();
    public DbSet<AgentKnowledgeBase> AgentKnowledgeBases => Set<AgentKnowledgeBase>();
    public DbSet<McpServer> McpServers => Set<McpServer>();
    public DbSet<AgentMcpServer> AgentMcpServers => Set<AgentMcpServer>();
    public DbSet<ConsumerApplication> ConsumerApplications => Set<ConsumerApplication>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<AgentSkill> AgentSkills => Set<AgentSkill>();
    public DbSet<AgentSubAgent> AgentSubAgents => Set<AgentSubAgent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.ToTable("agents");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).HasMaxLength(255).IsRequired();
            entity.HasIndex(a => a.Name).IsUnique();
            entity.Property(a => a.Provider).HasMaxLength(20).IsRequired();
            entity.Property(a => a.LlmModel).HasMaxLength(100).IsRequired();
            entity.Property(a => a.SystemPrompt).IsRequired();
            entity.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(a => a.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("sessions");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Title).HasMaxLength(255).IsRequired();
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(s => s.Agent)
                .WithMany(a => a.Sessions)
                .HasForeignKey(s => s.AgentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Role).HasMaxLength(20).IsRequired();
            entity.Property(m => m.Content).IsRequired();
            entity.Property(m => m.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(m => m.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).HasMaxLength(100).IsRequired();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(u => u.CreatedByUser)
                .WithMany()
                .HasForeignKey(u => u.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<KnowledgeBase>(entity =>
        {
            entity.ToTable("knowledge_bases");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Name).HasMaxLength(255).IsRequired();
            entity.HasIndex(k => k.Name).IsUnique();
            entity.Property(k => k.Description).IsRequired();
            entity.Property(k => k.EmbeddingProvider).HasMaxLength(20).IsRequired();
            entity.Property(k => k.EmbeddingModel).HasMaxLength(100).IsRequired();
            entity.Property(k => k.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(k => k.UpdatedAt).HasDefaultValueSql("now()");
            entity.Ignore(k => k.WeaviateCollectionName);

            entity.HasOne(k => k.CreatedByUser)
                .WithMany()
                .HasForeignKey(k => k.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KbDocument>(entity =>
        {
            entity.ToTable("kb_documents");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.FileName).HasMaxLength(500).IsRequired();
            entity.Property(d => d.ContentType).HasMaxLength(200).IsRequired();
            entity.Property(d => d.ObjectKey).HasMaxLength(1000).IsRequired();
            entity.Property(d => d.Status).HasMaxLength(20).IsRequired();
            entity.Property(d => d.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.KnowledgeBase)
                .WithMany(k => k.Documents)
                .HasForeignKey(d => d.KnowledgeBaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KbChunkRef>(entity =>
        {
            entity.ToTable("kb_chunk_refs");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(c => c.KbDocument)
                .WithMany(d => d.Chunks)
                .HasForeignKey(c => c.KbDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentKnowledgeBase>(entity =>
        {
            entity.ToTable("agent_knowledge_bases");
            entity.HasKey(l => new { l.AgentId, l.KnowledgeBaseId });

            entity.HasOne(l => l.Agent)
                .WithMany(a => a.KnowledgeBaseLinks)
                .HasForeignKey(l => l.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.KnowledgeBase)
                .WithMany(k => k.AgentLinks)
                .HasForeignKey(l => l.KnowledgeBaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<McpServer>(entity =>
        {
            entity.ToTable("mcp_servers");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(255).IsRequired();
            entity.HasIndex(s => s.Name).IsUnique();
            entity.Property(s => s.Kind).HasMaxLength(20).IsRequired();
            entity.Property(s => s.Url).HasMaxLength(2000);
            entity.Property(s => s.AuthType).HasMaxLength(20).IsRequired();
            entity.Property(s => s.BuiltInKey).HasMaxLength(100);
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AgentMcpServer>(entity =>
        {
            entity.ToTable("agent_mcp_servers");
            entity.HasKey(l => new { l.AgentId, l.McpServerId });

            entity.HasOne(l => l.Agent)
                .WithMany(a => a.McpServerLinks)
                .HasForeignKey(l => l.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.McpServer)
                .WithMany(s => s.AgentLinks)
                .HasForeignKey(l => l.McpServerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConsumerApplication>(entity =>
        {
            entity.ToTable("consumer_applications");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).HasMaxLength(255).IsRequired();
            entity.Property(a => a.ClientId).HasMaxLength(100).IsRequired();
            entity.HasIndex(a => a.ClientId).IsUnique();
            entity.Property(a => a.ClientSecretHash).IsRequired();
            entity.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("skills");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(255).IsRequired();
            entity.HasIndex(s => s.Name).IsUnique();
            entity.Property(s => s.Description).IsRequired();
            entity.Property(s => s.Content).IsRequired();
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AgentSkill>(entity =>
        {
            entity.ToTable("agent_skills");
            entity.HasKey(l => new { l.AgentId, l.SkillId });

            entity.HasOne(l => l.Agent)
                .WithMany(a => a.SkillLinks)
                .HasForeignKey(l => l.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.Skill)
                .WithMany(s => s.AgentLinks)
                .HasForeignKey(l => l.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentSubAgent>(entity =>
        {
            entity.ToTable("agent_sub_agents");
            entity.HasKey(l => new { l.AgentId, l.SubAgentId });

            // Self-referencing N:N — only one side may cascade to avoid an ambiguous multi-path delete.
            entity.HasOne(l => l.Agent)
                .WithMany(a => a.SubAgentLinks)
                .HasForeignKey(l => l.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.SubAgent)
                .WithMany()
                .HasForeignKey(l => l.SubAgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
