using CKYC.Server.Data;
using CKYC.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CKYC.Server.Controllers;

[ApiController]
[Route("api/inbound")]
public sealed class InboundStatusController : ControllerBase
{
    private readonly CkycDbContext _db;

    public InboundStatusController(CkycDbContext db)
    {
        _db = db;
    }

    [HttpGet("status/{requestRef}")]
    public async Task<IActionResult> GetStatus(string requestRef, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestRef))
            return BadRequest(new { message = "requestRef is required." });

        requestRef = requestRef.Trim();

        var sub = await _db.InboundSubmissions
            .AsNoTracking()
            .Where(s => s.RequestRef == requestRef)
            .OrderByDescending(s => s.ReceivedAtUtc)
            .Select(s => new
            {
                s.RequestRef,
                StatusCode = (int)s.Status,
                Status = s.Status.ToString(),
                s.StatusMessage,
                s.FailureReason,
                s.CkycNumber,
                s.ReceivedAtUtc,
                s.ProcessedAtUtc
            })
            .FirstOrDefaultAsync(ct);

        if (sub == null)
            return NotFound(new { requestRef, message = "No inbound submission found for this RequestRef." });

        return Ok(sub);
    }
}