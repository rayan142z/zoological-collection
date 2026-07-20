using System.ComponentModel.DataAnnotations.Schema;

namespace Zoolog.Models;

public class Specimen
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? DateCollected { get; set; }
    public string? Status { get; set; }
    public string? Size { get; set; }
    public string? PhotoPath { get; set; }
    [Column("location_id")]
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public int TaxonomyId { get; set; }
    public Taxonomy Taxonomy { get; set; } = null!;
    public int CollectionId { get; set; }
    public Collection Collection { get; set; } = null!;
    public int AddedBy { get; set; }
    public User AddedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    [Column("weight")] 
    public decimal? Weight { get; set; }
    [Column("birth_year")]
    public int? BirthYear { get; set; }
}