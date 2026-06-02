namespace Zoolog.Models;

public class LoanModel
{
    public int Id { get; set; }
    public int? SpecimenId { get; set; }
    public SpecimenModel Specimen { get; set; }
    public string LoanedTo { get; set; }
    public DateOnly LoanDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}