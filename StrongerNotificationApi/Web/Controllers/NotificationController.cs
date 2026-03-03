using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stronger.Api.Controllers;
using StrongerNotificationApi.Application.UseCases.Commands;

namespace StrongerNotificationApi.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController(IMediator mediator) : BaseController(mediator)
    {
        [HttpPost]
        public Task<IActionResult> AddAsync(AddUserDeviceCommand command, CancellationToken cancellation)
        {
            return this.SendAsync(command, cancellation);
        } 
    }
}
