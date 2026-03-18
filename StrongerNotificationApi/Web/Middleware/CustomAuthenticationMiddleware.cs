using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;

namespace StrongerNotificationApi.Web.Middleware;

public class CustomAuthenticationMiddleware : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationHandler
{
    private readonly IConfiguration _config;
    public CustomAuthenticationMiddleware
    (
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration
    ) : base(options, logger, encoder) => _config = configuration;
    

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("ApiKey", out var apiKeyValues))
                return AuthenticateResult.NoResult();

        var apiKey = apiKeyValues.FirstOrDefault();

        if (String.IsNullOrWhiteSpace(apiKey))
            return AuthenticateResult.Fail("Unauthorized. Check ApiKey in Header is correct.");

        var configKey = _config["ApiKey"];

        ArgumentException.ThrowIfNullOrWhiteSpace(configKey, nameof(configKey));

        if(apiKey != configKey)
            return AuthenticateResult.Fail("Unauthorized. Check ApiKey in Header is correct.");
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "StrongerWebApi")
        };
        
        var identity = new ClaimsIdentity(claims, "ApiKey");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
