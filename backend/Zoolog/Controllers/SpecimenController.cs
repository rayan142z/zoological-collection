using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
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
    // GET /api/specimen
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
    // GET /api/specimen/{id}
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

    // POST /api/specimen
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SpecimenRequest request)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var isPrivileged = User.IsInRole("admin") || User.IsInRole("moderator");
        
        var collection = await _db.Collections.FindAsync(request.CollectionId);
        if (collection is null)
        {
            return BadRequest(new { message = "Die Sammlung existiert nicht." });
        }

        if (!isPrivileged && collection.CreatedBy != userId)
        {
            return Forbid();
        }

        if (!await _db.Locations.AnyAsync(location => location.Id == request.LocationId))
        {
            return BadRequest(new { message = "Der Fundort existiert nicht." });
        }

        if (!await _db.Taxonomies.AnyAsync(taxonomy => taxonomy.Id == request.TaxonomyId))
        {
            return BadRequest(new { message = "Die Taxonomie existiert nicht." });
        }

        var specimen = new Specimen
        {
            Name = request.Name,
            Description = request.Description,
            DateCollected = request.DateCollected ?? DateOnly.FromDateTime(DateTime.Now),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "available" : request.Status,
            Size = request.Size,
            PhotoPath = request.PhotoPath,
            LocationId = request.LocationId,
            TaxonomyId = request.TaxonomyId,
            CollectionId = request.CollectionId,
            AddedBy = userId,
            CreatedAt = DateTime.Now
        };

        _db.Specimens.Add(specimen);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = specimen.Id }, ToResponse(specimen));
    }

    // PUT /api/specimen/{id}
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SpecimenRequest request)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var specimen = await _db.Specimens.FindAsync(id);
        if (specimen is null)
        {
            return NotFound();
        }

        var isPrivileged = User.IsInRole("admin") || User.IsInRole("moderator");

        if (!isPrivileged && specimen.AddedBy != userId)
        {
            return Forbid();
        }

        var collection = await _db.Collections.FindAsync(request.CollectionId);
        if (collection is null)
        {
            return BadRequest(new { message = "Die Sammlung existiert nicht." });
        }

        if (!isPrivileged && collection.CreatedBy != userId)
        {
            return Forbid();
        }

        if (!await _db.Locations.AnyAsync(location => location.Id == request.LocationId))
        {
            return BadRequest(new { message = "Der Fundort existiert nicht." });
        }

        if (!await _db.Taxonomies.AnyAsync(taxonomy => taxonomy.Id == request.TaxonomyId))
        {
            return BadRequest(new { message = "Die Taxonomie existiert nicht." });
        }

        specimen.Name = request.Name;
        specimen.Description = request.Description;
        specimen.DateCollected = request.DateCollected ?? specimen.DateCollected;
        specimen.Status = string.IsNullOrWhiteSpace(request.Status) ? specimen.Status : request.Status;
        specimen.Size = request.Size;
        specimen.PhotoPath = request.PhotoPath;
        specimen.LocationId = request.LocationId;
        specimen.TaxonomyId = request.TaxonomyId;
        specimen.CollectionId = request.CollectionId;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /api/specimen/{id}
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var specimen = await _db.Specimens.FindAsync(id);
        if (specimen is null)
        {
            return NotFound();
        }

        var isPrivileged = User.IsInRole("admin") || User.IsInRole("moderator");

        if (!isPrivileged && specimen.AddedBy != userId)
        {
            return Forbid();
        }

        _db.Specimens.Remove(specimen);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdText, out userId);
    }

    private static object ToResponse(Specimen specimen)
    {
        return new
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
        };
    }
}

public class SpecimenRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly? DateCollected { get; set; }

    [StringLength(30)]
    [RegularExpression("^(available|on loan|lost|destroyed)$", ErrorMessage = "Status muss einer der folgenden Werte sein: available, on loan, lost, destroyed.")]
    public string? Status { get; set; }

    [StringLength(100)]
    public string? Size { get; set; }

    [StringLength(500)]
    public string? PhotoPath { get; set; }

    [Range(1, int.MaxValue)]
    public int LocationId { get; set; }

    [Range(1, int.MaxValue)]
    public int TaxonomyId { get; set; }

    [Range(1, int.MaxValue)]
    public int CollectionId { get; set; }
}