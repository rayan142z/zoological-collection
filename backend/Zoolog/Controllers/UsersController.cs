using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoolog;
using Zoolog.Models;
using BCrypt.Net;

namespace Zoolog.Controllers;

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
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    // GET /api/users/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    // POST /api/users
    [HttpPost]
    public async Task<IActionResult> Create(User user)
    {
        user.Pass = BCrypt.Net.BCrypt.HashPassword(user.Pass);
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }
    
    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] User loginDaten)
    {
        
        var user = await _context.Users.FirstOrDefaultAsync(u => 
            u.Username == loginDaten.Username || u.Email == loginDaten.Username);

        if (user is null)
        {
            return Unauthorized(new { message = "Ungültige Anmeldedaten" });
        }

       
        bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(loginDaten.Pass, user.Pass);

        if (!isPasswordCorrect)
        {
            return Unauthorized(new { message = "Ungültige Anmeldedaten" });
        }

        return Ok(new { message = "Login erfolgreich", username = user.Username });
    }

    // PUT /api/users/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, User updatedUser)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.Username = updatedUser.Username;
        user.Email = updatedUser.Email;
        user.Pass = BCrypt.Net.BCrypt.HashPassword(updatedUser.Pass);
        user.UserRole = updatedUser.UserRole;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/users/1
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