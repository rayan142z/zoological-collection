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
                    collection.Creator.Status,
                    
                    collection.Creator.Description,
                    collection.Creator.Job
                },
                collection.CreatedAt
            })
            .ToListAsync();
        return Ok(collections);
    }

    // POST /api/collections/5/favorite
    [HttpPost("{id}/favorite")]
    public async Task<IActionResult> AddFavorite(int id, [FromBody] int userId)
    {
        // Prüfen, ob der Favorit schon existiert
        var exists = await _context.CollectionFavorites
            .AnyAsync(cf => cf.UserId == userId && cf.CollectionId == id);

        if (exists)
            return BadRequest("Sammlung ist bereits als Favorit markiert.");

        var favorite = new CollectionFavorite
        {
            UserId = userId,
            CollectionId = id,
            FavoritedAt = DateTime.UtcNow
        };

        _context.CollectionFavorites.Add(favorite);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Favorit erfolgreich hinzugefügt." });
    }

    // DELETE /api/collections/5/favorite/user/1
    [HttpDelete("{id}/favorite/user/{userId}")]
    public async Task<IActionResult> RemoveFavorite(int id, int userId)
    {
        var favorite = await _context.CollectionFavorites
            .FirstOrDefaultAsync(cf => cf.UserId == userId && cf.CollectionId == id);

        if (favorite == null)
            return NotFound("Favorit nicht gefunden.");

        _context.CollectionFavorites.Remove(favorite);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Favorit erfolgreich entfernt." });
    }

    // GET /api/collections/favorites/user/1
    [HttpGet("favorites/user/{userId}")]
    public async Task<IActionResult> GetUserFavorites(int userId)
    {
        var favorites = await _context.CollectionFavorites
            .Where(cf => cf.UserId == userId)
            .Select(cf => cf.CollectionId) // Gibt direkt ein Array der favorisierten Collection-IDs zurück
            .ToListAsync();

        return Ok(favorites);
    }

   [AllowAnonymous]
    [HttpGet("search-public")]
    public async Task<IActionResult> SearchPublic([FromQuery] string? query = "")
    {
        // Startpunkt: Nur Sammlungen, die explizit öffentlich sichtbar sind
        var collectionsQuery = _context.Collections.Where(c => c.IsPublic);

        // Suchbegriff filtern (falls übergeben)
        if (!string.IsNullOrWhiteSpace(query))
        {
            var cleanQuery = query.Trim().ToLower();
            // SQL Server arbeitet standardmäßig Case-Insensitive, ToLower() sichert es ab
            collectionsQuery = collectionsQuery.Where(c => c.Name.ToLower().Contains(cleanQuery));
        }

        var results = await collectionsQuery
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
                    collection.Creator.Status,
                    collection.Creator.Description,
                    collection.Creator.Job
                },
                collection.CreatedAt,
                // Ermittelt live die Anzahl der enthaltenen Exemplare für die Suchübersicht
                SpecimenCount = _context.Specimens.Count(s => s.CollectionId == collection.Id)
            })
            .ToListAsync();

        return Ok(results);
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
                    collection.Creator.Status,
                    collection.Creator.Description,
                    collection.Creator.Job
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
        //collection.IsPublic = request.IsPublic;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/collections/1
    //[Authorize(Roles = "admin,moderator")]
   [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCollection(int id)
    {
        // 1. Prüfen, ob die Sammlung überhaupt existiert
        var collection = await _context.Collections.FindAsync(id);
        
        if (collection == null) 
        {
            return NotFound(new { message = "Sammlung mit dieser ID nicht gefunden" });
        }

        // 2. Prüfen, ob sich in dieser Sammlung Exemplare befinden, die aktuell verliehen sind (Status != 'returned')
        bool hasActiveLoans = await _context.Loans
            .Join(_context.Specimens,
                loan => loan.SpecimenId,
                specimen => specimen.Id,
                (loan, specimen) => new { loan, specimen })
            .AnyAsync(x => x.specimen.CollectionId == id && x.loan.Status != "returned");

        if (hasActiveLoans)
        {
            return BadRequest(new { message = "Die Sammlung kann nicht gelöscht werden, da sich darin Exemplare befinden, die aktuell verliehen sind." });
        }

        try
        {
            // 3. Erst alle alten Leihvorgänge löschen, die zu den Exemplaren dieser Sammlung gehören
            await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM loan WHERE specimen_id IN (SELECT id FROM specimen WHERE collection_id = {0})", id);

            // 4. Danach die Exemplare und die Sammlung löschen
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM specimen WHERE collection_id = {0}", id);
            _context.Collections.Remove(collection);

            // 5. Änderungen in der Datenbank speichern
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Löschen: {ex.Message}");
            return StatusCode(500, new { message = "Löschen fehlgeschlagen", detail = ex.Message });
        }
    }

    [HttpGet("public-stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicStats()
    {
        var totalCollections = await _context.Collections.CountAsync(c => c.IsPublic);
        var totalObjects = await _context.Specimens.CountAsync();
        var totalUsers = await _context.Users.CountAsync();

        return Ok(new
        {
            totalCollections,
            totalObjects,
            totalUsers
        });
    }
}