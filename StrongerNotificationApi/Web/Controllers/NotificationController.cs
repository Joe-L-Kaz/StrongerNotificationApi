using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stronger.Api.Controllers;
using StrongerNotificationApi.Application.UseCases.Commands;

namespace StrongerNotificationApi.Web.Controllers
{
    public class NotificationController(IMediator mediator) : BaseController(mediator)
    {
        [HttpPost]
        [ActionName("Add")]
        public Task<IActionResult> AddAsync(AddUserDeviceCommand command, CancellationToken cancellation)
        {
            return this.SendAsync(command, cancellation);
        }

        [HttpPost]
        [ActionName("SendNotifs")]
        [Route("/api/[controller]/[action]")]
        public Task<IActionResult> SendMassAsync(CancellationToken cancellationToken)
        {
            return this.SendAsync(new SendNotificationsCommand(), cancellationToken);
        }

        [HttpPut]
        [ActionName("SetTrainingDays")]
        public Task<IActionResult> SetTrainingDaysAsync([FromQuery] Guid userId, SetTrainingDaysCommand command ,CancellationToken cancellationToken)
        {
            command.UserId = userId;
            return this.SendAsync(command, cancellationToken);
        }

    }
}
