using System.Reflection;
using AutoMapper;
using StrongerNotificationApi.Application.Config;
namespace StrongerNotificationApi.Application.Extensions;

public static class ApplicationLayerExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        return services
                    .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
                    .AddAutoMapper(cfg => {}, typeof(MapperProfile));

    }

}
