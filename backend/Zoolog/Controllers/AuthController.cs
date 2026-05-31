using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoolog;
using Zoolog.Models;
using BCrypt.Net;

namespace Zoolog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Group6DbContext _context;

    public AuthController(Group6DbContext context)
    {
        _context = context;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        
        var user = await _context.Users.FirstOrDefaultAsync(u => 
            u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail);

        
        if (user is null)
        {
            return Unauthorized(new { message = "Ungültige Anmeldedaten." });
        }

        
        bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, user.Pass);

        if (!isPasswordCorrect)
        {
            return Unauthorized(new { message = "Ungültige Anmeldedaten." });
        }
        
        return Ok(new { 
            message = "Login erfolgreich", 
            userId = user.Id, 
            username = user.Username,
            role = user.UserRole 
        });
    }
}