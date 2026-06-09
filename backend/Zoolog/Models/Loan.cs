namespace Zoolog.Models;

public class Loan
{
    public int Id { get; set; }
    public int? SpecimenId { get; set; }
    public Specimen Specimen { get; set; }
    public string LoanedTo { get; set; }
    public DateOnly LoanDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}