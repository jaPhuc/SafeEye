using SafeEye.Domain.Entities;
using SafeEye.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace SafeEye.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<IoTDevice> IoTDevices => Set<IoTDevice>();
    public DbSet<GuardianDevice> GuardianDevices => Set<GuardianDevice>();
    public DbSet<SosEvent> SosEvents => Set<SosEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}