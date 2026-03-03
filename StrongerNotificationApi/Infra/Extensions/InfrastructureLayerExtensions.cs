using Microsoft.EntityFrameworkCore;
using StrongerNotificationApi.Application.Abstractions.Repositories;
using StrongerNotificationApi.Infra.Persistence;
using StrongerNotificationApi.Infra.Persistence.Repositories;

namespace Stronger.Infrastructure;

public static class InfrastructureLayerExtensions
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDbContext<StrongerNotifDbContext>(options =>
            {
                options.UseMySql(configuration.GetConnectionString("MySql")!, ServerVersion.AutoDetect(configuration.GetConnectionString("MySql")));
            })
            .AddScoped<IStrongerNotifDbContext, StrongerNotifDbContext>()
            .AddScoped<IUserDeviceRepository, UserDeviceRepository>();
            
        return services;
    }

}
