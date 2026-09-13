using Microsoft.EntityFrameworkCore;
using DeviceManagementService.Models;

namespace DeviceManagementService.Data;

public class DeviceDbContext(DbContextOptions<DeviceDbContext> options) : DbContext(options)
{
    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Module> Modules => Set<Module>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasIndex(d => d.SerialNumber).IsUnique();
            entity.HasIndex(d => d.HouseId);
            entity.HasOne(d => d.Type)
                .WithMany()
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeviceType>().HasIndex(t => t.Name).IsUnique();

        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasIndex(m => m.DeviceId);
            entity.HasOne(m => m.Device)
                .WithMany()
                .HasForeignKey(m => m.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
