using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Contracts;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/ping")]
public sealed class PingController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PingResponse), StatusCodes.Status200OK)]
    public ActionResult<PingResponse> Get()
    {
        var response = new PingResponse(
            Message: "TaskManager API is running.",
            TimestampUtc: DateTimeOffset.UtcNow);

        return Ok(response);
    }
}