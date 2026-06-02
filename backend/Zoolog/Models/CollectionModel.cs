namespace Zoolog.Models;

public class CollectionModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int CreatedBy { get; set; }
    public UserModel Creator { get; set; }
    public DateTime CreatedAt { get; set; }
}