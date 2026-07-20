using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoolog.Models;

namespace Zoolog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoanController : ControllerBase
{
    private readonly Group6DbContext _db;

    public LoanController(Group6DbContext db)
    {
        _db = db;
    }

    // GET api/loan — get all loans
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var loans = await _db.Loans
            .Include(l => l.Specimen)  // include specimen details
            .ToListAsync();
        return Ok(loans);
    }

    // GET api/loan/5 — get a specific loan
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var loan = await _db.Loans
            .Include(l => l.Specimen)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null) return NotFound();
        return Ok(loan);
    }

    // POST api/loan — create a new loan
    [HttpPost]
    public async Task<IActionResult> CreateLoan([FromBody] Loan loan)
    {
        // check the specimen exists
        var specimen = await _db.Specimens.FindAsync(loan.SpecimenId);
        if (specimen == null)
            return BadRequest("Specimen not found.");

        // check the specimen is available
        if (specimen.Status != "available")
            return BadRequest($"Specimen is not available (current status: {specimen.Status}).");

        // mark specimen as on loan
        specimen.Status = "on loan";

        loan.LoanDate = DateOnly.FromDateTime(DateTime.Today);

        _db.Loans.Add(loan);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, loan);
    }

    // PUT api/loan/5/return — return a loaned specimen
    [HttpPut("{id}/return")]
    public async Task<IActionResult> ReturnLoan(int id)
    {
        var loan = await _db.Loans
            .Include(l => l.Specimen)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null) return NotFound();
        if (loan.Status == "returned")
            return BadRequest("This loan has already been returned.");

        // mark loan as returned
        loan.Status = "returned";
        loan.ReturnDate = DateOnly.FromDateTime(DateTime.Today);

        // mark specimen as available again
        if (loan.Specimen != null)
            loan.Specimen.Status = "available";

        await _db.SaveChangesAsync();
        return Ok(loan);
    }

    // GET api/loan/overdue — get all overdue loans
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var overdue = await _db.Loans
            .Include(l => l.Specimen)
            .Where(l => l.Status == "active" && l.ReturnDate < today)
            .ToListAsync();

        return Ok(overdue);
    }
}