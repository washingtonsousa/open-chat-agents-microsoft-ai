using Microsoft.EntityFrameworkCore;
using OpenChatAgents.Api.Models;

namespace OpenChatAgents.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Message> Messages => Set<Message>();

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
    }
}
