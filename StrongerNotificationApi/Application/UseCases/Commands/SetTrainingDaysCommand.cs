using System;
using System.Text.Json.Serialization;
using MediatR;
using Stronger.Domain.Responses;
using StrongerNotificationApi.Application.Responses.UserDevice;

namespace StrongerNotificationApi.Application.UseCases.Commands;

public class SetTrainingDaysCommand : IRequest<Response<SetTrainingDaysResponse>>
{
    [JsonIgnore]
    public Guid UserId {get; set;}
    public short BitMask {get; set;}
}
