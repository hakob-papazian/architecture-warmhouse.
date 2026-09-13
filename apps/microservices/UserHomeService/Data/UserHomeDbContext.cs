using Microsoft.EntityFrameworkCore;
using UserHomeService.Models;

namespace UserHomeService.Data;

public class UserHomeDbContext(DbContextOptions<UserHomeDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<House> Houses => Set<House>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<House>(entity =>
        {
            entity.HasOne(h => h.Owner)
                .WithMany(u => u.Houses)
                .HasForeignKey(h => h.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
