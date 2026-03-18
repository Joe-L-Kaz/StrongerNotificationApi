using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Stronger.Api.BackgroundWorkers;
using Stronger.Infrastructure;
using StrongerNotificationApi.Application.Extensions;
using StrongerNotificationApi.Infra.Persistence;
using StrongerNotificationApi.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "CustomAuthentication";
}).AddScheme<AuthenticationSchemeOptions, CustomAuthenticationMiddleware>("CustomAuthentication", options => { });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services
    .AddApplicationLayer()
    .AddInfrastructureLayer(builder.Configuration);

builder.Services.AddHostedService<DailyNotificationsWorker>();

var app = builder.Build();

await ApplyMigrationsWithRetryAsync(app.Services, app.Logger);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

static async Task ApplyMigrationsWithRetryAsync(IServiceProvider services, ILogger logger)
{
    const int maxAttempts = 10;
    var delay = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<StrongerNotifDbContext>();

            logger.LogInformation("Applying database migrations (attempt {Attempt}/{Max})...", attempt, maxAttempts);
            
            await db.Database.MigrateAsync();

            logger.LogInformation("Database migrations applied successfully.");
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Migration attempt {Attempt} failed. Retrying in {Delay}...", attempt, delay);
            await Task.Delay(delay);
        }
    }

    throw new InvalidOperationException("Failed to apply database migrations after multiple attempts.");
}