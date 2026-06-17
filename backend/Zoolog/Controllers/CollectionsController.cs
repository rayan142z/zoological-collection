using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoolog;
using Zoolog.Models;

namespace Zoolog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollectionsController : ControllerBase
{
    private readonly Group6DbContext _context;

    public CollectionsController(Group6DbContext context)
    {
        _context = context;
    }

    // GET /api/collections
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var collections = await _context.Collections
            .Include(c => c.Creator)
            .ToListAsync();
        return Ok(collections);
    }

    // GET /api/collections/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var collection = await _context.Collections
            .Include(c => c.Creator)
            .FirstOrDefaultAsync(c => c.Id == id);
        return collection is null ? NotFound() : Ok(collection);
    }

    // POST /api/collections
    [HttpPost]
    public async Task<IActionResult> Create(Collection collection)
    {
        collection.CreatedAt = DateTime.Now;
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = collection.Id }, collection);
    }

    // PUT /api/collections/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Collection updatedCollection)
    {
        var collection = await _context.Collections.FindAsync(id);
        if (collection is null) return NotFound();

        collection.Name = updatedCollection.Name;
        collection.Description = updatedCollection.Description;
        collection.IsPublic = updatedCollection.IsPublic;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/collections/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var collection = await _context.Collections.FindAsync(id);
        if (collection is null) return NotFound();

        _context.Collections.Remove(collection);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}