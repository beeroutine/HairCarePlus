using System.Text.Json;
using HairCarePlus.Shared.Communication.Sync;
using HairCarePlus.Server.Application.PhotoReports;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using HairCarePlus.Shared.Communication;
using HairCarePlus.Server.Infrastructure.Data;
using System.Linq;
using System;
using HairCarePlus.Server.Application.Sync;

namespace HairCarePlus.Server.API.Controllers;

[ApiController]
[Route("sync")] 
public sealed class SyncController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db;

    public SyncController(IMediator mediator, AppDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    [HttpPost("batch")]
    public async Task<IActionResult> Batch([FromBody] BatchSyncRequestDto dto)
        {
        var resp = await _mediator.Send(new BatchSyncCommand(dto));
        return Ok(resp);
    }
} 