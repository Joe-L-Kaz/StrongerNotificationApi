using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StrongerNotificationApi.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        [HttpGet]
        public Task<String> RegisterAsync(CancellationToken cancellation)
        {
            return Task.FromResult("Success");
        } 
    }
}
