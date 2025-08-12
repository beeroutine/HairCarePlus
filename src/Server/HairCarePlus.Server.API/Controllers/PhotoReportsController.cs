using System.Collections.Generic;
using System.Threading.Tasks;
using HairCarePlus.Server.Application.PhotoReports;
using HairCarePlus.Shared.Communication;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;

namespace HairCarePlus.Server.API.Controllers;

[ApiController]
[Route("[controller]")]
    public class PhotoReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PhotoReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{patientId}")]
    public async Task<ActionResult<IReadOnlyList<PhotoReportDto>>> GetReports(string patientId)
    {
        if(!Guid.TryParse(patientId, out var pid))
            return BadRequest("Invalid patientId");

        // Ephemeral policy: do not expose historical photo reports via GET.
        // Return empty to prevent clients from building history from the server.
        await Task.CompletedTask;
        return Ok(System.Array.Empty<PhotoReportDto>());
    }

    // Legacy single-photo endpoint kept for backward compatibility during migration.
    [HttpPost]
    public async Task<ActionResult<PhotoReportDto>> CreateReport([FromBody] CreatePhotoReportRequest body)
    {
        var dto = await _mediator.Send(new CreatePhotoReportCommand(Guid.Parse(body.PatientId), body.ImageUrl, body.Date));
        return Ok(dto);
    }

    // New endpoint: accept a single PhotoReportSet (three photos atomically)
    [HttpPost("set")]
    public async Task<ActionResult<HairCarePlus.Shared.Communication.PhotoReportSetDto>> CreateReportSet([FromBody] HairCarePlus.Shared.Communication.PhotoReportSetDto set)
    {
        var created = await _mediator.Send(new HairCarePlus.Server.Application.PhotoReports.CreatePhotoReportSetCommand(set));
        return Ok(created);
    }

    [HttpPost("{patientId}/{photoReportId}/comments")]
    public async Task<ActionResult<PhotoCommentDto>> AddComment(string patientId, string photoReportId, [FromBody] AddCommentRequest body)
    {
        var dto = await _mediator.Send(new AddPhotoCommentCommand(Guid.Parse(patientId), Guid.Parse(photoReportId), Guid.Parse(body.AuthorId), body.Text));
        return Ok(dto);
    }

    public sealed record CreatePhotoReportRequest(string PatientId, string ImageUrl, System.DateTime Date);
    public sealed record AddCommentRequest(string AuthorId, string Text);
} 