using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Zoolog.Models;

namespace Zoolog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecimenController : ControllerBase
{
    private readonly Group6DbContext _db;

    public SpecimenController(Group6DbContext db)
    {
        _db = db;
    }

    // Public users may only see specimens from public collections.
    // Logged-in users may see public specimens plus specimens they added themselves.
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isLoggedIn = int.TryParse(userIdText, out var userId);

        var specimens = await _db.Specimens
            .Where(specimen =>
                specimen.Collection.IsPublic ||
                (isLoggedIn && specimen.AddedBy == userId))
            .Select(specimen => new
            {
                specimen.Id,
                specimen.Name,
                specimen.Description,
                specimen.DateCollected,
                specimen.Status,
                specimen.Size,
                specimen.PhotoPath,
                specimen.LocationId,
                specimen.TaxonomyId,
                specimen.CollectionId,
                specimen.AddedBy,
                specimen.CreatedAt
            })
            .ToListAsync();
        return Ok(specimens);
    }
    // Same visibility rule as GetAll:
    // public collection OR own added specimen.
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isLoggedIn = int.TryParse(userIdText, out var userId);

        var specimen = await _db.Specimens
            .Where(specimen =>
                specimen.Id == id &&
                (specimen.Collection.IsPublic ||
                (isLoggedIn && specimen.AddedBy == userId)))
            .Select(specimen => new
            {
                specimen.Id,
                specimen.Name,
                specimen.Description,
                specimen.DateCollected,
                specimen.Status,
                specimen.Size,
                specimen.PhotoPath,
                specimen.LocationId,
                specimen.TaxonomyId,
                specimen.CollectionId,
                specimen.AddedBy,
                specimen.CreatedAt
            })
            .FirstOrDefaultAsync();
            
        if (specimen == null) return NotFound();
        return Ok(specimen);
    }
}