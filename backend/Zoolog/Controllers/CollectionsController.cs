using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Zoolog;
using Zoolog.Models;
using System.Security.Claims;

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
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var collections = await _context.Collections
            .Select(collection => new
            {
                collection.Id,
                collection.Name,
                collection.Description,
                collection.IsPublic,
                collection.CreatedBy,
                Creator = new
                {
                    collection.Creator.Id,
                    collection.Creator.Username,
                    collection.Creator.Email,
                    collection.Creator.UserRole,
                    collection.Creator.Status
                },
                collection.CreatedAt
            })
            .ToListAsync();
        return Ok(collections);
    }

    // GET /api/collections/1
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var collection = await _context.Collections
            .Where(collection => collection.Id == id)
            .Select(collection => new
            {
                collection.Id,
                collection.Name,
                collection.Description,
                collection.IsPublic,
                collection.CreatedBy,
                Creator = new
                {
                    collection.Creator.Id,
                    collection.Creator.Username,
                    collection.Creator.Email,
                    collection.Creator.UserRole,
                    collection.Creator.Status
                },
                collection.CreatedAt
            })
            .FirstOrDefaultAsync();
        return collection is null ? NotFound() : Ok(collection);
    }

    // POST /api/collections
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(Collection collection)
    {
        // The creator of a collection must come from the authenticated JWT,
        // not from the request body. Otherwise a client could send any CreatedBy
        // value and create a collection in the name of another user.
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdText, out var userId))
        {
            return Unauthorized();
        }

        collection.CreatedBy = userId;
        collection.CreatedAt = DateTime.Now;
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = collection.Id }, new
        {
            collection.Id,
            collection.Name,
            collection.Description,
            collection.IsPublic,
            collection.CreatedBy,
            collection.CreatedAt
        });
    }

    // PUT /api/collections/1
    [Authorize]
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
    [Authorize(Roles = "admin,moderator")]
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