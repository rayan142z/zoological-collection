using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var specimens = await _db.Specimens.ToListAsync();
        return Ok(specimens);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var specimen = await _db.Specimens.FindAsync(id);
        if (specimen == null) return NotFound();
        return Ok(specimen);
    }
}