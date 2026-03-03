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

/// <summary>
/// Minimal APNs HTTP/2 client with JWT provider-token generation (ES256) and in-memory caching.
/// Token lifetime is ~1 hour; we refresh slightly early to avoid edge failures.
/// </summary>
public sealed class ApnsApiClient : IApnsApiClient
{
    private readonly HttpClient _client;

    private readonly string _apnsTopic;
    private readonly string _teamId;
    private readonly string _keyId;
    private readonly string _p8Path;

    // Cached provider token
    private string? _cachedJwt;
    private DateTimeOffset _cachedJwtExpiresAtUtc;

    // Prevent concurrent refreshes
    private readonly SemaphoreSlim _jwtLock = new(1, 1);

    public ApnsApiClient(IConfiguration configuration)
    {
        // Required config
        var baseUrl = configuration["Apns:BaseUrl"] ?? configuration["ApnsUrl"]; // allow legacy key
        _apnsTopic = configuration["Apns:Topic"] ?? configuration["ApnsTopic"] ?? throw new InvalidOperationException("Missing config: Apns:Topic (bundle id).");
        _teamId = configuration["Apns:TeamId"] ?? configuration["ApnsTeamId"] ?? throw new InvalidOperationException("Missing config: Apns:TeamId.");
        _keyId = configuration["Apns:KeyId"] ?? configuration["ApnsKeyId"] ?? throw new InvalidOperationException("Missing config: Apns:KeyId.");
        _p8Path = configuration["Apns:P8Path"] ?? configuration["ApnsP8Path"] ?? throw new InvalidOperationException("Missing config: Apns:P8Path.");

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Missing config: Apns:BaseUrl (e.g. https://api.sandbox.push.apple.com).");

        // APNs requires HTTP/2; make this the default for all requests.
        var handler = new SocketsHttpHandler
        {
            // Proxies can force HTTP/1.1; disable unless you explicitly need one.
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

        // Build minimal alert payload
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
            // BaseAddress already ends with '/', so this becomes: 3/device/<token>
            RequestUri = new Uri($"3/device/{deviceToken}", UriKind.Relative),
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        // Required headers
        request.Headers.TryAddWithoutValidation("authorization", $"bearer {jwt}");
        request.Headers.TryAddWithoutValidation("apns-topic", _apnsTopic);
        request.Headers.TryAddWithoutValidation("apns-push-type", "alert");

        // Optional but recommended
        request.Headers.TryAddWithoutValidation("apns-priority", "10");

        using var response = await _client.SendAsync(request, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        response.Headers.TryGetValues("apns-id", out var apnsIdValues);
        var apnsId = apnsIdValues is null ? null : string.Join(",", apnsIdValues);

        return (response.StatusCode, body, apnsId);
    }

    private async Task<string> GetOrCreateJwtAsync(CancellationToken cancellationToken)
    {
        // Refresh if missing or expiring within 5 minutes
        var now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(_cachedJwt) && _cachedJwtExpiresAtUtc > now.AddMinutes(5))
            return _cachedJwt!;

        await _jwtLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            now = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(_cachedJwt) && _cachedJwtExpiresAtUtc > now.AddMinutes(5))
                return _cachedJwt!;

            // iat in seconds
            var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Header and payload for APNs provider token
            var headerJson = $"{{\"alg\":\"ES256\",\"kid\":\"{_keyId}\"}}";
            var payloadJson = $"{{\"iss\":\"{_teamId}\",\"iat\":{iat}}}";

            var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            var signingInput = $"{headerB64}.{payloadB64}";

            var signature = SignEs256(signingInput);

            _cachedJwt = $"{signingInput}.{Base64UrlEncode(signature)}";

            // Apple tokens are valid for up to 1 hour; refresh at ~55 minutes.
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
        // Read .p8 key (PKCS#8 EC private key) and sign with ECDSA using SHA-256.
        // Cache the parsed key if you want; for simplicity, we read each refresh (~hourly).
        var p8 = File.ReadAllText(_p8Path);

        // Strip PEM armor
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

        // IMPORTANT: Use IEEE P1363 format (r||s) for JWT signatures.
        return ecdsa.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        // Base64Url per RFC 7515
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
