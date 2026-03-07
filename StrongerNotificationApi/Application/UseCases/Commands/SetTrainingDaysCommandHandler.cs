using System;
using MediatR;
using Stronger.Domain.Responses;
using StrongerNotificationApi.Application.Abstractions.Repositories;
using StrongerNotificationApi.Application.Responses.UserDevice;
using StrongerNotificationApi.Domain.Entities;

namespace StrongerNotificationApi.Application.UseCases.Commands;

public class SetTrainingDaysCommandHandler(IUserDeviceRepository repository, IStrongerNotifDbContext context) : IRequestHandler<SetTrainingDaysCommand, Response<SetTrainingDaysResponse>>
{
    async Task<Response<SetTrainingDaysResponse>> IRequestHandler<SetTrainingDaysCommand, Response<SetTrainingDaysResponse>>.Handle(SetTrainingDaysCommand request, CancellationToken cancellationToken)
    {
        IEnumerable<UserDeviceEntity> userDevices = await repository.RetrieveAllByIdAsync(request.UserId, cancellationToken);
        foreach(var device in userDevices)
        {
            device.TrainingDays = Convert.ToByte(request.BitMask);
            repository.Update(device);
        }

        await context.SaveChangesAsync(cancellationToken);

        return new Response<SetTrainingDaysResponse>
        {
            StatusCode = 204
        };
    }
}
