using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Zoolog;
using Zoolog.Models;
using System.Security.Claims;

namespace Zoolog.Controllers;

public class CollectionRequest
{
    [Required(ErrorMessage = "Name ist erforderlich.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Name muss zwischen 2 und 150 Zeichen lang sein.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Beschreibung darf maximal 1000 Zeichen lang sein.")]
    public string? Description { get; set; }

    public bool IsPublic { get; set; } = false;
}

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
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isLoggedIn = int.TryParse(userIdText, out var userId);

        var collections = await _context.Collections
            .Where(collection =>
                collection.IsPublic ||
                (isLoggedIn && collection.CreatedBy == userId))
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
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isLoggedIn = int.TryParse(userIdText, out var userId);

        var collection = await _context.Collections
            .Where(collection =>
                collection.Id == id &&
                (collection.IsPublic || (isLoggedIn && collection.CreatedBy == userId)))
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
    public async Task<IActionResult> Create(CollectionRequest request)
    {
        // The creator of a collection must come from the authenticated JWT,
        // not from the request body. Otherwise a client could send any CreatedBy
        // value and create a collection in the name of another user.
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdText, out var userId))
        {
            return Unauthorized();
        }

        var collection = new Collection
        {
            Name = request.Name,
            Description = request.Description,
            IsPublic = request.IsPublic,
            CreatedBy = userId,
            CreatedAt = DateTime.Now
        };

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
    public async Task<IActionResult> Update(int id, CollectionRequest request)
    {
        var collection = await _context.Collections.FindAsync(id);
        if (collection is null) return NotFound();

        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isOwner = int.TryParse(userIdText, out var userId) && collection.CreatedBy == userId;
        var isAdmin = User.IsInRole("admin");
        var isModeratorOnPublic = User.IsInRole("moderator") && collection.IsPublic;

        if (!isOwner && !isAdmin && !isModeratorOnPublic)
        {
            return Forbid();
        }

        collection.Name = request.Name;
        collection.Description = request.Description;
        collection.IsPublic = request.IsPublic;

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

        if (User.IsInRole("moderator") && !collection.IsPublic)
        {
            return Forbid();
        }

        _context.Collections.Remove(collection);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}