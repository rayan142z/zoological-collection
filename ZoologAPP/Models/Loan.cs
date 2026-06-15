namespace ZoologAPP.Models;

public class Loan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ObjectId { get; set; } = "";
    public string BorrowerName { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime BorrowedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public bool IsOpen => ReturnedAt is null;
}
