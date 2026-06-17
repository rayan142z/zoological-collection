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

    // Optional: leave empty/omit to keep the current password.
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
    [Authorize(Roles = "admin")]
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
                user.CreatedAt
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
    [Authorize(Roles="admin")]
    [HttpPut("{id}")]
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

        if (!string.IsNullOrWhiteSpace(request.Pass))
        {
            user.Pass = BCrypt.Net.BCrypt.HashPassword(request.Pass);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/users/1
    [Authorize(Roles="admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}