using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Zoolog.Models;

namespace Zoolog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly Group6DbContext _db;

    public LocationController(Group6DbContext db)
    {
        _db = db;
    }

    // GET /api/location
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var locations = await _db.Locations
            .Select(location => new
            {
                location.Id,
                location.Name,
                location.Latitude,
                location.Longitude,
                location.Country,
                location.Region
            })
            .ToListAsync();
        return Ok(locations);
    }

    // GET /api/location/1
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var location = await _db.Locations
            .Where(location => location.Id == id)
            .Select(location => new
            {
                location.Id,
                location.Name,
                location.Latitude,
                location.Longitude,
                location.Country,
                location.Region
            })
            .FirstOrDefaultAsync();
        return location is null ? NotFound() : Ok(location);
    }
}