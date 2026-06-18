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
                taxonomy.Species
            })
            .ToListAsync();
        return Ok(taxonomies);
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
                taxonomy.Species
            })
            .FirstOrDefaultAsync();
        return taxonomy is null ? NotFound() : Ok(taxonomy);
    }
}