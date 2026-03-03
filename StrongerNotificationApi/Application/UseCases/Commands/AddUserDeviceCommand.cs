
using MediatR;
using Stronger.Domain.Responses;
using StrongerNotificationApi.Application.Responses.UserDevice;
using StrongerNotificationApi.Domain.Enums;

namespace StrongerNotificationApi.Application.UseCases.Commands;

public record AddUserDeviceCommand(
    Guid UserId,
    String DeviceToken,
    DeviceType DeviceType,
    byte TrainingDays,
    String FirstName
) : IRequest<Response<AddUserDeviceResponse>>;
    