namespace Zoolog.Models;

public class Collection
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int CreatedBy { get; set; }
    public User Creator { get; set; }
    public DateTime CreatedAt { get; set; }
}