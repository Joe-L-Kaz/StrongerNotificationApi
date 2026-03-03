using System;
using System.Net;

namespace StrongerNotificationApi.Application.Abstractions.Services;

public interface IApnsApiClient
{
    Task<(HttpStatusCode StatusCode, string Body, string? ApnsId)> SendAsync(
        string firstName,
        string deviceToken,
        CancellationToken cancellationToken = default
    );
}
