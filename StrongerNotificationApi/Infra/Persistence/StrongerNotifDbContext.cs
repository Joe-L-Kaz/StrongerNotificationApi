using System;
using Microsoft.EntityFrameworkCore;
using StrongerNotificationApi.Application.Abstractions.Repositories;
using StrongerNotificationApi.Domain.Entities;
using StrongerNotificationApi.Infra.Config;

namespace StrongerNotificationApi.Infra.Persistence;

public class StrongerNotifDbContext(DbContextOptions<StrongerNotifDbContext> options) : DbContext(options), IStrongerNotifDbContext
{
    public DbSet<UserDeviceEntity> UserDevices { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new UserDeviceEntityTypeConfiguration().Configure(modelBuilder.Entity<UserDeviceEntity>());
        base.OnModelCreating(modelBuilder);
    }

}
