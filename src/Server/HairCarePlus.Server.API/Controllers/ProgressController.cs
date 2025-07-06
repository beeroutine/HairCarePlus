using HairCarePlus.Server.Application.Progress;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HairCarePlus.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProgressController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProgressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{patientId}")]
    public async Task<IActionResult> Get(string patientId)
    {
        if (!Guid.TryParse(patientId, out var pid))
            return BadRequest("Invalid patient id");

        var results = await _mediator.Send(new GetProgressEntriesQuery(pid));
        return Ok(results);
    }
} 