namespace Zoolog.Models;

public class Specimen
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateOnly? DateCollected { get; set; }
    public string? Status { get; set; }
    public int LocationId { get; set; }
    public Location Location { get; set; }
    public int TaxonomyId { get; set; }
    public Taxonomy Taxonomy { get; set; }
    public int CollectionId { get; set; }
    public Collection Collection { get; set; }
    public int AddedBy { get; set; }
    public User AddedByUser { get; set; }
    public DateTime CreatedAt { get; set; }
}