namespace ZoologAPP.Models;

public class StorageLocation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Building { get; set; } = "";
    public string Room { get; set; } = "";
    public string Shelf { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
