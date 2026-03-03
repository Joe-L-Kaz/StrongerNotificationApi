
using AutoMapper;
using MediatR;
using Stronger.Domain.Responses;
using StrongerNotificationApi.Application.Abstractions.Repositories;
using StrongerNotificationApi.Application.Responses.UserDevice;
using StrongerNotificationApi.Domain.Entities;

namespace StrongerNotificationApi.Application.UseCases.Commands;

public class AddUserDeviceCommandHandler(
    IUserDeviceRepository repository,
    IStrongerNotifDbContext context,
    IMapper mapper
) : IRequestHandler<AddUserDeviceCommand, Response<AddUserDeviceResponse>>
{
    async Task<Response<AddUserDeviceResponse>> IRequestHandler<AddUserDeviceCommand, Response<AddUserDeviceResponse>>.Handle(AddUserDeviceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        UserDeviceEntity entity = mapper.Map<UserDeviceEntity>(request);

        await repository.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new Response<AddUserDeviceResponse>
        {
            StatusCode = 201
        };
    }
}
