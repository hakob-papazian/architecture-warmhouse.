using Microsoft.EntityFrameworkCore;
using HeatingControlService.Models;

namespace HeatingControlService.Data;

public class HeatingDbContext(DbContextOptions<HeatingDbContext> options) : DbContext(options)
{
    public DbSet<HeatingProfile> Profiles => Set<HeatingProfile>();
    public DbSet<HeatingCommand> Commands => Set<HeatingCommand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HeatingProfile>(entity =>
        {
            entity.HasKey(p => p.HouseId);
            entity.Property(p => p.Mode).HasConversion<string>();
            entity.Property(p => p.CurrentState).HasConversion<string>();
        });

        modelBuilder.Entity<HeatingCommand>(entity =>
        {
            entity.HasIndex(c => c.HouseId);
            entity.Property(c => c.Action).HasConversion<string>();
            entity.Property(c => c.Source).HasConversion<string>();
        });
    }
}
