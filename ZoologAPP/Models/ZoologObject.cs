namespace ZoologAPP.Models;

public class ZoologObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string CollectionId { get; set; } = "";
    public string LocationId { get; set; } = "";
    public string Status { get; set; } = "verfügbar"; // verfügbar, ausgeliehen, verloren, zerstört
    public string Notes { get; set; } = "";

    // Taxonomie
    public string Reich { get; set; } = "";
    public string Stamm { get; set; } = "";
    public string Klasse { get; set; } = "";
    public string Ordnung { get; set; } = "";
    public string Familie { get; set; } = "";
    public string Gattung { get; set; } = "";
    public string Art { get; set; } = "";

    public string PhotoPath { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
