namespace ZoologAPP.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "user"; // user, moderator, admin
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
