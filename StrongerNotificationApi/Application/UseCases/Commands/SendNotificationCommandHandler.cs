using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Stronger.Domain.Responses;
using StrongerNotificationApi.Application.Abstractions.Repositories;
using StrongerNotificationApi.Application.Abstractions.Services;
using StrongerNotificationApi.Application.Responses.UserDevice;
using StrongerNotificationApi.Domain.Entities;
using StrongerNotificationApi.Infra.Services;

namespace StrongerNotificationApi.Application.UseCases.Commands;

public class SendNotificationCommandHandler(IUserDeviceRepository repository, IApnsApiClient apnsClient)
    : IRequestHandler<SendNotificationsCommand, Response<SendNotificationsResponse>>
{
    public async Task<Response<SendNotificationsResponse>> Handle(
        SendNotificationsCommand request,
        CancellationToken cancellationToken
    )
    {
        // TODO: replace with request.UserId (or whichever id you intend) once wired
        UserDeviceEntity? entity = await repository.RetrieveAsync(
            new Guid("08de2c3c-8258-4de1-8a6f-eb5a3dff89af"),
            cancellationToken
        );

        if (entity is null || string.IsNullOrWhiteSpace(entity.DeviceToken))
        {
            return new Response<SendNotificationsResponse>
            {
                StatusCode = 404,
                Error = new Response<SendNotificationsResponse>.ErrorModel
                {
                    StatusCode = 404,
                    Message = "Device token not found for user."
                }
            };
        }

        HttpStatusCode statusCode;
        string apnsBody;
        string? apnsId;

        try
        {
            (statusCode, apnsBody, apnsId) = await apnsClient.SendAsync(
                entity.FirstName ?? "there",
                entity.DeviceToken,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            return new Response<SendNotificationsResponse>
            {
                StatusCode = 500,
                Error = new Response<SendNotificationsResponse>.ErrorModel
                {
                    StatusCode = 500,
                    Message = $"Failed to call APNs: {ex.Message}"
                }
            };
        }

        // APNs success is 200 OK; errors typically include JSON like {"reason":"BadDeviceToken"}
        if ((int)statusCode < 200 || (int)statusCode > 299)
        {
            return new Response<SendNotificationsResponse>
            {
                StatusCode = (int)statusCode,
                Error = new Response<SendNotificationsResponse>.ErrorModel
                {
                    StatusCode = (int)statusCode,
                    Message = string.IsNullOrWhiteSpace(apnsBody)
                        ? $"APNs returned {(int)statusCode}."
                        : $"APNs error: {apnsBody}"
                }
            };
        }

        return new Response<SendNotificationsResponse>
        {
            StatusCode = 200
        };
    }
}
