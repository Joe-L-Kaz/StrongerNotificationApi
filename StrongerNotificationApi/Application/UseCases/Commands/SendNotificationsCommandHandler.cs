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
        
        byte todayMask = GetTodayTrainingMaskLondon();

        IEnumerable<UserDeviceEntity> devices = await repository.RetrieveAllTrainingTodayAsync(todayMask, cancellationToken);
            
        foreach(var device in devices)
        {
            _ = apnsClient.SendAsync(
                device.FirstName ?? "there",
                device.DeviceToken,
                cancellationToken
            );
        }

        return new Response<SendNotificationsResponse>
        {
            StatusCode = 200,
        };
    }

    private static byte GetTodayTrainingMaskLondon()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        var nowLondon = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);

        // .NET: Sunday=0, Monday=1, ... Saturday=6
        int bitIndex = nowLondon.DayOfWeek switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            DayOfWeek.Sunday => 6,
            _ => throw new ArgumentOutOfRangeException(nameof(DayOfWeek), $"Unexpected day of week: {nowLondon.DayOfWeek}")
        };

        return (byte)(1 << bitIndex);
    }
}
