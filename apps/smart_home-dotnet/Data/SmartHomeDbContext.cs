using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Models;

namespace SmartHome.Api.Data;

public class SmartHomeDbContext : DbContext
{
    public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options) : base(options)
    {
    }

    public DbSet<Sensor> Sensors => Set<Sensor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.ToTable("sensors");

            entity.HasKey(s => s.Id);

            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(s => s.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            entity.Property(s => s.Location).HasColumnName("location").HasMaxLength(100).IsRequired();
            entity.Property(s => s.Value).HasColumnName("value").HasDefaultValue(0);
            entity.Property(s => s.Unit).HasColumnName("unit").HasMaxLength(20);
            entity.Property(s => s.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue("inactive");
            entity.Property(s => s.LastUpdated).HasColumnName("last_updated").IsRequired();
            entity.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(s => s.Type).HasDatabaseName("idx_sensors_type");
            entity.HasIndex(s => s.Location).HasDatabaseName("idx_sensors_location");
            entity.HasIndex(s => s.Status).HasDatabaseName("idx_sensors_status");
        });
    }
}
