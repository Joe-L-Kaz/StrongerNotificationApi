using System;
using System.ComponentModel.DataAnnotations;
using StrongerNotificationApi.Domain.Enums;

namespace StrongerNotificationApi.Domain.Entities;

public class UserDeviceEntity
{
    [Key]
    public Guid UserId { get; set;}
    public String? DeviceToken { get; set; } = String.Empty;
    public DeviceType DeviceType { get; set; }
    public byte TrainingDays {get; set;}
    public String FirstName { get; set;} = String.Empty;
}
