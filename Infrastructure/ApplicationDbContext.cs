using CMSApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace CMSApi.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<CmsEntity> CmsEntities => Set<CmsEntity>();
    public DbSet<CmsEntityVersion> CmsEntityVersions => Set<CmsEntityVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CmsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.HasMany(e => e.Versions)
                  .WithOne(v => v.CmsEntity)
                  .HasForeignKey(v => v.CmsEntityId);
        });

        modelBuilder.Entity<CmsEntityVersion>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Version).IsRequired();
            entity.Property(v => v.Timestamp).IsRequired();
            entity.Property(v => v.Payload).HasColumnType("nvarchar(max)");
        });
    }
}