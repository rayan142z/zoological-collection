namespace Zoolog.Models;

public class SpecimenModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateOnly? DateCollected { get; set; }
    public string? Status { get; set; }
    public int LocationId { get; set; }
    public LocationModel Location { get; set; }
    public int TaxonomyId { get; set; }
    public TaxonomyModel Taxonomy { get; set; }
    public int CollectionId { get; set; }
    public CollectionModel Collection { get; set; }
    public int AddedBy { get; set; }
    public UserModel AddedByUser { get; set; }
    public DateTime CreatedAt { get; set; }
}