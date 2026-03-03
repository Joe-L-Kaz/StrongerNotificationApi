using System;
using MediatR;
using Stronger.Domain.Responses;
using StrongerNotificationApi.Application.Responses.UserDevice;

namespace StrongerNotificationApi.Application.UseCases.Commands;

public record SendNotificationsCommand : IRequest<Response<SendNotificationsResponse>>;