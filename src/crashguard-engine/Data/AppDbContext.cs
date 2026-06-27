using Crashguard.Engine.Models;
using Microsoft.EntityFrameworkCore;

namespace Crashguard.Engine.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Canary> Canaries => Set<Canary>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<CanaryCheckpoint> CanaryCheckpoints => Set<CanaryCheckpoint>();
    public DbSet<CanaryType> CanaryTypes => Set<CanaryType>();
    public DbSet<CanaryTypeRule> CanaryTypeRules => Set<CanaryTypeRule>();
    public DbSet<CanaryTypeChannel> CanaryTypeChannels => Set<CanaryTypeChannel>();
    public DbSet<CanaryAlertBatch> CanaryAlertBatches => Set<CanaryAlertBatch>();
    public DbSet<Settings> Settings => Set<Settings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Canary>(entity =>
        {
            entity.HasIndex(c => new { c.CanaryType, c.ReferenceId }).IsUnique();
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<CanaryCheckpoint>(entity =>
        {
            entity.HasIndex(c => c.CanaryId);
        });

        modelBuilder.Entity<CanaryType>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasMany(c => c.Rules).WithOne().HasForeignKey(r => r.CanaryTypeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(c => c.DefaultChannels).WithOne().HasForeignKey(c => c.CanaryTypeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CanaryTypeRule>(entity =>
        {
            entity.HasIndex(r => r.CanaryTypeId);
        });

        modelBuilder.Entity<CanaryTypeChannel>(entity =>
        {
            entity.HasIndex(c => c.CanaryTypeId);
            entity.HasOne<Channel>().WithMany().HasForeignKey(c => c.ChannelId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(c => new { c.CanaryTypeId, c.ChannelId }).IsUnique();
        });

        modelBuilder.Entity<CanaryAlertBatch>(entity =>
        {
            entity.HasIndex(b => new { b.CanaryTypeId, b.Channel, b.Severity, b.IsOpen });
            entity.HasOne<CanaryType>().WithMany().HasForeignKey(b => b.CanaryTypeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Settings>(entity =>
        {
            entity.HasData(new Settings { Id = 1 });
        });
    }
}
