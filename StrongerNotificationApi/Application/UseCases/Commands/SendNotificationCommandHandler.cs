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
using StrongerNotificationApi.Application.Responses.UserDevice;
using StrongerNotificationApi.Domain.Entities;

namespace StrongerNotificationApi.Application.UseCases.Commands;

public class SendNotificationCommandHandler(IUserDeviceRepository repository)
    : IRequestHandler<SendNotificationsCommand, Response<SendNotificationsResponse>>
{
    // TODO: Move these into configuration (appsettings / env vars) once you've proven APNs works.
    // For now, hardcode to prove "proof of life".
    private const string ApnsHost = "https://api.sandbox.push.apple.com/3/device/";
    private const string ApnsTopic = "com.strongermobileapp.ios"; // <-- REPLACE with your iOS app Bundle Identifier
    private const string JwtBearerToken = "eyJhbGciOiJFUzI1NiIsImtpZCI6IjRXTDlSRzQ4V1MifQ.eyJpc3MiOiJERjY3WlFMR0pCIiwiaWF0IjoxNzcyNTUzNTAxfQ.0Bl6qQG4AXbY_AYUqDgVJo0xoUyhB9ZWYc3Exl2sMEZsYq1HvfyFCeXHqxW8I_eZKuRt27ewc6OlgKivHWebBQ"; // <-- REPLACE with a valid ES256 APNs provider token

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

        // APNs requires HTTP/2. HttpClient uses HTTP/2 automatically over TLS when supported.
        using HttpClient client = new();
        
        var payload = new
        {
            aps = new
            {
                alert = new
                {
                    title = "Hello",
                    body = "Test from my API"
                },
                sound = "default"
            }
        };

        using var httpRequest = new HttpRequestMessage
        {
            Version = HttpVersion.Version20,
            Method = HttpMethod.Post,
            RequestUri = new Uri(ApnsHost + entity.DeviceToken),
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            )
        };

        // Required headers
        httpRequest.Headers.TryAddWithoutValidation("authorization", $"bearer {JwtBearerToken}");
        httpRequest.Headers.TryAddWithoutValidation("apns-topic", ApnsTopic);
        httpRequest.Headers.TryAddWithoutValidation("apns-push-type", "alert");

        // Optional but recommended
        httpRequest.Headers.TryAddWithoutValidation("apns-priority", "10");

        HttpResponseMessage apnsResponse;
        string apnsBody;

        try
        {
            apnsResponse = await client.SendAsync(httpRequest, cancellationToken);
            apnsBody = await apnsResponse.Content.ReadAsStringAsync(cancellationToken);
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
        if (!apnsResponse.IsSuccessStatusCode)
        {
            return new Response<SendNotificationsResponse>
            {
                StatusCode = (int)apnsResponse.StatusCode,
                Error = new Response<SendNotificationsResponse>.ErrorModel
                {
                    StatusCode = (int)apnsResponse.StatusCode,
                    Message = string.IsNullOrWhiteSpace(apnsBody)
                        ? $"APNs returned {(int)apnsResponse.StatusCode}."
                        : $"APNs error: {apnsBody}"
                }
            };
        }

        // Useful for tracing; APNs returns an apns-id header for requests
        apnsResponse.Headers.TryGetValues("apns-id", out var apnsIdValues);
        var apnsId = apnsIdValues is null ? null : string.Join(",", apnsIdValues);

        return new Response<SendNotificationsResponse>
        {
            StatusCode = 200
        };
    }
}
