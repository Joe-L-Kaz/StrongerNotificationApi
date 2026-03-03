using Microsoft.AspNetCore.Mvc;

using MediatR;

using Stronger.Domain.Responses;
using Microsoft.AspNetCore.Authorization;

namespace Stronger.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("/api/[controller]")]
    public abstract class BaseController(IMediator _mediator) : ControllerBase
    {
        private protected async Task<IActionResult> SendAsync<T>(
            IRequest<Response<T>> request,
            CancellationToken cancellationToken
        ) where T : class
        {
            Response<T> response = await _mediator.Send(request, cancellationToken);

            if (response.Content is not null)
                return StatusCode(response.StatusCode, response.Content);

            if (response.Error is not null)
                return StatusCode(response.StatusCode, response.Error);

            return StatusCode(response.StatusCode);
        }
    }
}
