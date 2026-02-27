using Bank.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bank.Web.Controllers;

[ApiController]
public class LookupsApiController : ControllerBase
{
    private readonly BankDbContext _context;
    public LookupsApiController(BankDbContext context) => _context = context;

    // GET /api/counties
    [HttpGet("/api/counties")]
    public async Task<IActionResult> GetCounties()
    {
        var data = await _context.Counties
            .Where(c => c.IsActive)
            .OrderBy(c => c.CountyName)
            .Select(c => new { id = c.CountyId, name = c.CountyName })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("/api/cities")]
    public async Task<IActionResult> GetCities([FromQuery] Guid countyId)
    {
        if (countyId == Guid.Empty)
            return BadRequest(new { error = "countyId is required" });

        // ✅ verify the county exists (helps debugging)
        var countyExists = await _context.Counties.AnyAsync(c => c.CountyId == countyId);
        if (!countyExists)
            return NotFound(new { error = "County not found", countyId });

        // ✅ IMPORTANT: City must have a CountyId FK column
        var data = await _context.Cities
            .Where(c => c.IsActive && c.CountyId == countyId) // <-- FK MUST match your schema
            .OrderBy(c => c.CityName)
            .Select(c => new { id = c.CityId, name = c.CityName })
            .ToListAsync();

        return Ok(data);
    }
}