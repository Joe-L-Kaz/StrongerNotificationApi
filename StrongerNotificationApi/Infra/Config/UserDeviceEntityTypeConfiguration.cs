using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongerNotificationApi.Domain.Entities;

namespace StrongerNotificationApi.Infra.Config;

public class UserDeviceEntityTypeConfiguration : IEntityTypeConfiguration<UserDeviceEntity>
{
    public void Configure(EntityTypeBuilder<UserDeviceEntity> builder)
    {
        builder
            .Property(u => u.DeviceType)
            .HasConversion<String>();
    }
}
