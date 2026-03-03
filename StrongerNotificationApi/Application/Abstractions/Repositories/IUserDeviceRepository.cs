using System;
using StrongerNotificationApi.Domain.Entities;

namespace StrongerNotificationApi.Application.Abstractions.Repositories;

public interface IUserDeviceRepository
{
    Task AddAsync(UserDeviceEntity entity, CancellationToken cancellationToken);
    Task<UserDeviceEntity?> RetrieveAsync(Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<UserDeviceEntity>>  ListAsync(CancellationToken cancellationToken);
    void Update(UserDeviceEntity entity);
    void Delete(UserDeviceEntity entity);
}
