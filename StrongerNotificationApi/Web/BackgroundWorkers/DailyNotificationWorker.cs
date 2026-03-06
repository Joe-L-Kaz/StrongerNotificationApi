using MediatR;
using StrongerNotificationApi.Application.UseCases.Commands;

namespace Stronger.Api.BackgroundWorkers;

public sealed class DailyNotificationsWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyNotificationsWorker> _logger;

    private static readonly TimeSpan RunAt = new(12, 0, 0);

    public DailyNotificationsWorker(IServiceScopeFactory scopeFactory, ILogger<DailyNotificationsWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRunEuropeLondon(RunAt);

            _logger.LogInformation("DailyNotificationsWorker sleeping for {Delay} until next run.", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                _logger.LogInformation("DailyNotificationsWorker triggering SendNotificationsCommand...");

                await mediator.Send(new SendNotificationsCommand(), stoppingToken);

                _logger.LogInformation("DailyNotificationsWorker finished triggering SendNotificationsCommand.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DailyNotificationsWorker failed while sending notifications.");
            }
        }
    }

    private static TimeSpan GetDelayUntilNextRunEuropeLondon(TimeSpan runAtLocalTime)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);

        var todayRunLocal = new DateTimeOffset(
            nowLocal.Year, nowLocal.Month, nowLocal.Day,
            runAtLocalTime.Hours, runAtLocalTime.Minutes, runAtLocalTime.Seconds,
            nowLocal.Offset);

        var nextRun = todayRunLocal > nowLocal ? todayRunLocal : todayRunLocal.AddDays(1);
        return nextRun - nowLocal;
    }
}