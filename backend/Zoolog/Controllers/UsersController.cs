using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Zoolog;
using Zoolog.Models;
using BCrypt.Net;

namespace Zoolog.Controllers;

public class UserRequest
{
    [Required(ErrorMessage = "Benutzername ist erforderlich.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Benutzername muss zwischen 3 und 50 Zeichen lang sein.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-Mail ist erforderlich.")]
    [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Passwort ist erforderlich.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Passwort muss mindestens 6 Zeichen lang sein.")]
    public string Pass { get; set; } = string.Empty;
}

public class UserUpdateRequest
{
    [Required(ErrorMessage = "Benutzername ist erforderlich.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Benutzername muss zwischen 3 und 50 Zeichen lang sein.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-Mail ist erforderlich.")]
    [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rolle ist erforderlich.")]
    [RegularExpression("^(user|moderator|admin)$", ErrorMessage = "Rolle muss user, moderator oder admin sein.")]
    public string UserRole { get; set; } = "user";

    [Required(ErrorMessage = "Status ist erforderlich.")]
    [RegularExpression("^(active|blocked)$", ErrorMessage = "Status muss active oder blocked sein.")]
    public string Status { get; set; } = "active";

    // --- NEU: Optionale Beschreibung für das Benutzerprofil ---
    [StringLength(500, ErrorMessage = "Die Beschreibung darf maximal 500 Zeichen lang sein.")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "Job darf maximal 50 Zeichen lang sein.")]
    public string? Job { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Passwort muss mindestens 6 Zeichen lang sein.")]
    public string? Pass { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly Group6DbContext _context;

    public UsersController(Group6DbContext context)
    {
        _context = context;
    }

    // GET /api/users
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users
            .Select(user => new
            {
                user.Id,
                user.Username,
                user.Email,
                user.UserRole,
                user.Status,
                user.CreatedAt,
                user.Description, 
                user.Job
            })
            .ToListAsync();
        return Ok(users);
    }

    // GET /api/users/1
    [Authorize(Roles = "admin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.Users
            .Where(user => user.Id == id)
            .Select(user => new
            {
                user.Id,
                user.Username,
                user.Email,
                user.UserRole,
                user.Status,
                
                // --- NEU: Hier mitsenden ---
                user.Description, 
                user.Job, 
                
                user.CreatedAt
            })
            .FirstOrDefaultAsync();
        return user is null ? NotFound() : Ok(user);
    }

    // POST /api/users
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create(UserRequest request)
    {
        var alreadyExists = await _context.Users
            .AnyAsync(u => u.Username == request.Username || u.Email == request.Email);

        if (alreadyExists)
        {
            return Conflict(new { message = "Benutzername oder E-Mail wird bereits verwendet." });
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            Pass = BCrypt.Net.BCrypt.HashPassword(request.Pass),
            UserRole = "user",
            Status = "active",
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new
        {
            user.Id,
            user.Username,
            user.Email,
            user.UserRole,
            user.Status,
            user.CreatedAt
        });
    }

    // PUT /api/users/1
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, UserUpdateRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        var conflict = await _context.Users
            .AnyAsync(u => u.Id != id && (u.Username == request.Username || u.Email == request.Email));

        if (conflict)
        {
            return Conflict(new { message = "Benutzername oder E-Mail wird bereits verwendet." });
        }

        user.Username = request.Username;
        user.Email = request.Email;
        user.UserRole = request.UserRole;
        user.Status = request.Status;
        
        // --- NEU: Wert in der DB-Entität aktualisieren ---
        user.Description = request.Description; 
        user.Job = request.Job; 

        if (!string.IsNullOrWhiteSpace(request.Pass))
        {
            user.Pass = BCrypt.Net.BCrypt.HashPassword(request.Pass);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
    // DELETE /api/users/1
    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // 1. User suchen
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        // 2. Transaktion starten für maximale Datensicherheit
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 3. Alle Sammlungen laden, die diesem User gehören
            var userCollections = await _context.Collections
                .Where(c => c.CreatedBy == id)
                .ToListAsync();

            // 4. Erst die Sammlungen manuell aus dem Context entfernen
            if (userCollections.Any())
            {
                _context.Collections.RemoveRange(userCollections);
                // HINWEIS: Hier könntest du jetzt auch Schleifen einbauen,
                // um z.B. physische Bilder der Sammlungen von der Festplatte zu löschen!
            }

            // 5. Erst wenn die Sammlungen weg sind, den User selbst löschen
            _context.Users.Remove(user);

            // 6. Alle Änderungen in einem Rutsch in die DB schreiben
            await _context.SaveChangesAsync();

            // 7. Transaktion erfolgreich abschließen (Commit)
            await transaction.CommitAsync();

            return NoContent(); // 204
        }
        catch (Exception ex)
        {
            // Falls irgendetwas schiefgeht (z.B. DB-Verbindung bricht ab),
            // macht diese Zeile alle bisherigen Löschungen dieses Durchgangs rückgängig!
            await transaction.RollbackAsync();
            
            // Logge den Fehler für dich im Server-Terminal
            Console.WriteLine($"Fehler beim manuellen Löschen von User {id}: {ex.Message}");
            
            return StatusCode(500, "Fehler beim Löschen des Benutzers und seiner Daten.");
        }
    }
}