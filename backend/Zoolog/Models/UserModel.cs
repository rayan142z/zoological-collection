namespace Zoolog.Models;

public class UserModel
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Pass { get; set; }
    public string UserRole { get; set; }
    public DateTime CreatedAt { get; set; }
}