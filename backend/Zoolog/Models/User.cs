namespace Zoolog.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;
    public string UserRole { get; set; } = "user";
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }

    public string? Description { get; set; }
    public string? Job { get; set; }
}