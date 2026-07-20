using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Zoolog.Models;

namespace Zoolog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaxonomyController : ControllerBase
{
    private readonly Group6DbContext _db;

    public TaxonomyController(Group6DbContext db)
    {
        _db = db;
    }

    // GET /api/taxonomy
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var taxonomies = await _db.Taxonomies
            .Select(taxonomy => new
            {
                taxonomy.Id,
                taxonomy.Kingdom,
                taxonomy.Phylum,
                taxonomy.Class,
                taxonomy.Orders,
                taxonomy.Family,
                taxonomy.Genus,
                taxonomy.Species,
                taxonomy.Validated
            })
            .ToListAsync();
        return Ok(taxonomies);
    }

    [AllowAnonymous]
    [HttpGet("validated")]
    public async Task<IActionResult> GetValidated()
    {
        var taxonomy = await _db.Taxonomies
            .Where(taxonomy => taxonomy.Validated)
            .Select(taxonomy => new
            {
                taxonomy.Id,
                taxonomy.Kingdom,
                taxonomy.Phylum,
                taxonomy.Class,
                taxonomy.Orders,
                taxonomy.Family,
                taxonomy.Genus,
                taxonomy.Species,
                taxonomy.Validated
            })
            .FirstOrDefaultAsync();
        return taxonomy is null ? NotFound() : Ok(taxonomy);
    }

    [Authorize(Roles = "admin,moderator")]
    [HttpGet("unvalidated")]
    public async Task<IActionResult> GetUnvalidated()
    {
        var list = await _db.Taxonomies
            .Where(t => !t.Validated)
            .Select(taxonomy => new // Empfehlung: Direkt die gleiche Projektion wie bei den anderen nutzen
            {
                taxonomy.Id,
                taxonomy.Kingdom,
                taxonomy.Phylum,
                taxonomy.Class,
                taxonomy.Orders,
                taxonomy.Family,
                taxonomy.Genus,
                taxonomy.Species,
                taxonomy.Validated
            })
            .ToListAsync();
        return Ok(list);
    }

    // GET /api/taxonomy/1
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var taxonomy = await _db.Taxonomies
            .Where(taxonomy => taxonomy.Id == id)
            .Select(taxonomy => new
            {
                taxonomy.Id,
                taxonomy.Kingdom,
                taxonomy.Phylum,
                taxonomy.Class,
                taxonomy.Orders,
                taxonomy.Family,
                taxonomy.Genus,
                taxonomy.Species,
                taxonomy.Validated
            })
            .FirstOrDefaultAsync();
        return taxonomy is null ? NotFound() : Ok(taxonomy);
    }

    [Authorize(Roles = "admin,moderator")]
    [HttpPut("{id}/validate")]
    public async Task<IActionResult> ValidateTaxonomy(int id)
    {
        var taxonomy = await _db.Taxonomies.FindAsync(id);
        if (taxonomy is null) return NotFound(new { message = "Taxonomie nicht gefunden." });

        taxonomy.Validated = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Taxonomie erfolgreich validiert." });
    }
}