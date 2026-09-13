using Microsoft.EntityFrameworkCore;
using TemperatureMonitoringService.Models;

namespace TemperatureMonitoringService.Data;

public class TemperatureDbContext(DbContextOptions<TemperatureDbContext> options) : DbContext(options)
{
    public DbSet<TelemetryReading> Readings => Set<TelemetryReading>();
    public DbSet<WatchedSensor> WatchedSensors => Set<WatchedSensor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryReading>().HasIndex(r => new { r.HouseId, r.RecordedAt });
        modelBuilder.Entity<WatchedSensor>().HasIndex(w => w.HouseId);
    }
}
