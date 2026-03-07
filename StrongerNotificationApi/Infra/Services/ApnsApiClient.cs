using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StrongerNotificationApi.Application.Abstractions.Services;

namespace StrongerNotificationApi.Infra.Services;

public sealed class ApnsApiClient : IApnsApiClient
{
    private readonly HttpClient _client;

    private readonly string _apnsTopic;
    private readonly string _teamId;
    private readonly string _keyId;
    private readonly string _p8Path;

    private string? _cachedJwt;
    private DateTimeOffset _cachedJwtExpiresAtUtc;

    private readonly SemaphoreSlim _jwtLock = new(1, 1);

    public ApnsApiClient(IConfiguration configuration)
    {
        var baseUrl = configuration["Apns:BaseUrl"] ?? throw new InvalidOperationException("Missing config: Apns:BaseUrl.");
        _apnsTopic = configuration["Apns:Topic"] ?? configuration["ApnsTopic"] ?? throw new InvalidOperationException("Missing config: Apns:Topic (bundle id).");
        _teamId = configuration["Apns:TeamId"] ?? configuration["ApnsTeamId"] ?? throw new InvalidOperationException("Missing config: Apns:TeamId.");
        _keyId = configuration["Apns:KeyId"] ?? configuration["ApnsKeyId"] ?? throw new InvalidOperationException("Missing config: Apns:KeyId.");
        _p8Path = configuration["Apns:P8Path"] ?? configuration["ApnsP8Path"] ?? throw new InvalidOperationException("Missing config: Apns:P8Path.");

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Missing config: Apns:BaseUrl (e.g. https://api.sandbox.push.apple.com).");

        var handler = new SocketsHttpHandler
        {
            UseProxy = false
        };

        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
    }

    public async Task<(HttpStatusCode StatusCode, string Body, string? ApnsId)> SendAsync(
        string firstName,
        string deviceToken,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
            throw new ArgumentException("Device token is required.", nameof(deviceToken));

        var payload = new
        {
            aps = new
            {
                alert = new
                {
                    title = "Hello",
                    body = $"Hi {firstName}, time to train 💪"
                },
                sound = "default"
            }
        };

        var jwt = await GetOrCreateJwtAsync(cancellationToken);

        using var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"3/device/{deviceToken}", UriKind.Relative),
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.TryAddWithoutValidation("authorization", $"bearer {jwt}");
        request.Headers.TryAddWithoutValidation("apns-topic", _apnsTopic);
        request.Headers.TryAddWithoutValidation("apns-push-type", "alert");

        request.Headers.TryAddWithoutValidation("apns-priority", "10");

        using var response = await _client.SendAsync(request, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        response.Headers.TryGetValues("apns-id", out var apnsIdValues);
        var apnsId = apnsIdValues is null ? null : string.Join(",", apnsIdValues);

        return (response.StatusCode, body, apnsId);
    }

    private async Task<string> GetOrCreateJwtAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(_cachedJwt) && _cachedJwtExpiresAtUtc > now.AddMinutes(5))
            return _cachedJwt!;

        await _jwtLock.WaitAsync(cancellationToken);
        try
        {
            now = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(_cachedJwt) && _cachedJwtExpiresAtUtc > now.AddMinutes(5))
                return _cachedJwt!;

            var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var headerJson = $"{{\"alg\":\"ES256\",\"kid\":\"{_keyId}\"}}";
            var payloadJson = $"{{\"iss\":\"{_teamId}\",\"iat\":{iat}}}";

            var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            var signingInput = $"{headerB64}.{payloadB64}";

            var signature = SignEs256(signingInput);

            _cachedJwt = $"{signingInput}.{Base64UrlEncode(signature)}";

            _cachedJwtExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(55);

            return _cachedJwt!;
        }
        finally
        {
            _jwtLock.Release();
        }
    }

    private byte[] SignEs256(string signingInput)
    {
        var p8 = File.ReadAllText(_p8Path);

        const string begin = "-----BEGIN PRIVATE KEY-----";
        const string end = "-----END PRIVATE KEY-----";

        var start = p8.IndexOf(begin, StringComparison.Ordinal);
        var stop = p8.IndexOf(end, StringComparison.Ordinal);
        if (start < 0 || stop < 0)
            throw new InvalidOperationException("APNs .p8 key is not in expected PEM format.");

        var base64 = p8
            .Substring(start + begin.Length, stop - (start + begin.Length))
            .Replace("\r", "", StringComparison.Ordinal)
            .Replace("\n", "", StringComparison.Ordinal)
            .Trim();

        var keyBytes = Convert.FromBase64String(base64);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(keyBytes, out _);

        var data = Encoding.ASCII.GetBytes(signingInput);

        return ecdsa.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
