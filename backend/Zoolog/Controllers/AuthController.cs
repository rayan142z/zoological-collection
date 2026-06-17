using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Zoolog;
using Zoolog.Models;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Zoolog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Group6DbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(Group6DbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

        if (user.Status != "active")
        {
            return Unauthorized(new { message = "Dieser Nutzer ist gesperrt oder inaktiv." });
        }

        var token = GenerateJwtToken(user);
        
        return Ok(new { 
            message = "Login erfolgreich", 
            token,
            expiresInMinutes = 120,
            user = new
            {
                id = user.Id, 
                username = user.Username,
                email = user.Email,
                role = user.UserRole,
                status = user.Status 
            }
        });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "Zoolog";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "ZoologClient";

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("JWT key is missing.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.UserRole)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(120);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}