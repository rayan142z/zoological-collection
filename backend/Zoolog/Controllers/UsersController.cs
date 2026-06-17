using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<IActionResult> Create(User user)
    {
        user.Pass = BCrypt.Net.BCrypt.HashPassword(user.Pass);
        user.UserRole = "user";
        user.Status = "active";

        
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
    public async Task<IActionResult> Update(int id, User updatedUser)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.Username = updatedUser.Username;
        user.Email = updatedUser.Email;
        user.UserRole = updatedUser.UserRole;
        user.Status = updatedUser.Status;

        if (!string.IsNullOrWhiteSpace(updatedUser.Pass))
        {
            user.Pass = BCrypt.Net.BCrypt.HashPassword(updatedUser.Pass);
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