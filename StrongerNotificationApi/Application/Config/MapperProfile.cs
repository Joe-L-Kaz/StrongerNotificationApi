using AutoMapper;
using StrongerNotificationApi.Application.UseCases.Commands;
using StrongerNotificationApi.Domain.Entities;

namespace StrongerNotificationApi.Application.Config;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        this.CreateMap<AddUserDeviceCommand, UserDeviceEntity>().ReverseMap();
    }
}
