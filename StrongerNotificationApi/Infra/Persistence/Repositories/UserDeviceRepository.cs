using Microsoft.EntityFrameworkCore;
using StrongerNotificationApi.Application.Abstractions.Repositories;
using StrongerNotificationApi.Domain.Entities;

namespace StrongerNotificationApi.Infra.Persistence.Repositories;

public class UserDeviceRepository : IUserDeviceRepository
{
    private readonly IStrongerNotifDbContext _context;
    public UserDeviceRepository(IStrongerNotifDbContext context) => _context = context;

    async Task IUserDeviceRepository.AddAsync(UserDeviceEntity entity, CancellationToken cancellationToken)
    {
        await _context.UserDevices.AddAsync(entity, cancellationToken);
    }

    void IUserDeviceRepository.Delete(UserDeviceEntity entity)
    {
        _context.UserDevices.Remove(entity);
    }

    async Task<IEnumerable<UserDeviceEntity>> IUserDeviceRepository.ListAsync(CancellationToken cancellationToken)
    {
        return await _context.UserDevices.ToListAsync();
    }

    async Task<IEnumerable<UserDeviceEntity>> IUserDeviceRepository.RetrieveAllByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.UserDevices.Where(u => u.UserId == userId).ToListAsync(cancellationToken);
    }

    async Task<List<UserDeviceEntity>> IUserDeviceRepository.RetrieveAllTrainingTodayAsync(byte todayMask, CancellationToken cancellationToken)
    {
        return await _context.UserDevices.Where(u => (u.TrainingDays & todayMask) != 0).ToListAsync(cancellationToken);
    }

    async Task<UserDeviceEntity?> IUserDeviceRepository.RetrieveAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.UserDevices.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    void IUserDeviceRepository.Update(UserDeviceEntity entity)
    {
        _context.UserDevices.Update(entity);
    }
}
