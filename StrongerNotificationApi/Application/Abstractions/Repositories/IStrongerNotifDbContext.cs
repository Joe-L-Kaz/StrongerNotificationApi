using System;
using Microsoft.EntityFrameworkCore;
using StrongerNotificationApi.Domain.Entities;

namespace StrongerNotificationApi.Application.Abstractions.Repositories;

public interface IStrongerNotifDbContext
{
    DbSet<UserDeviceEntity> UserDevices {get;}
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
