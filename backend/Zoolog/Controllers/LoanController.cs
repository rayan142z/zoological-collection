using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
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

    // GET: /api/loan
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAllLoans()
    {
        var loans = await _db.Loans.ToListAsync();
        return Ok(loans);
    }

    // POST /api/loan
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JsonElement payload)
    {
        // 1. Die ID des Exemplars auslesen
        int specimenId = payload.GetProperty("specimenId").GetInt32();
        
        // 2. Geändert von String auf Int: Die ID der Person, an die verliehen wird
        int loanedTo = payload.GetProperty("loanedTo").GetInt32();
        
        // 3. Neu hinzugefügt: Die ID der Person/Stelle, von der verliehen wird (optional)
        int? loanedFrom = payload.TryGetProperty("loanedFrom", out var lf) && lf.ValueKind != JsonValueKind.Null 
            ? lf.GetInt32() 
            : null;

        string loanDateStr = payload.GetProperty("loanDate").GetString()!;

        string? returnDateStr = payload.TryGetProperty("returnDate", out var retDate) && retDate.ValueKind != JsonValueKind.Null 
            ? retDate.GetString() 
            : null;

        string? notes = payload.TryGetProperty("notes", out var n) && n.ValueKind != JsonValueKind.Null 
            ? n.GetString() 
            : null;

        int? fromCollection = payload.TryGetProperty("fromCollection", out var col) && col.ValueKind != JsonValueKind.Null 
            ? col.GetInt32() 
            : null;

        string rawStatus = payload.TryGetProperty("status", out var s) && s.ValueKind != JsonValueKind.Null 
            ? s.GetString() ?? "active"
            : "active";

        string status = rawStatus.ToLower() switch
        {
            "active" or "overdue" or "returned" => rawStatus.ToLower(),
            _ => "active"
        };

        // 4. Das Loan-Modell wird nun mit den IDs befüllt
        var loan = new Loan
        {
            SpecimenId = specimenId,
            LoanedTo = loanedTo,
            LoanedFrom = loanedFrom, // Hier wird die neue ID übergeben
            LoanDate = DateOnly.FromDateTime(DateTime.Parse(loanDateStr)),
            ReturnDate = returnDateStr != null ? DateOnly.FromDateTime(DateTime.Parse(returnDateStr)) : null,
            Notes = notes,
            FromCollection = fromCollection,
            Status = status
        };

        _db.Loans.Add(loan);

        // 5. Status des Exemplars in der Datenbank auf "verliehen" aktualisieren
        var specimen = await _db.Specimens.FindAsync(specimenId);
        if (specimen != null)
        {
            specimen.Status = "on loan"; 
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "Erfolgreich gespeichert und Exemplar als verliehen markiert" });
    }

    // POST: /api/loan/return/{id}
    [AllowAnonymous]
    [HttpPost("return/{id}")]
    public async Task<IActionResult> ReturnLoan(int id, [FromBody] JsonElement payload)
    {
        var loan = await _db.Loans.FindAsync(id);
        if (loan == null)
        {
            return NotFound(new { message = "Leihvorgang nicht gefunden." });
        }

        // Leihstatus auf "returned" setzen und heutiges Datum als Rückgabedatum eintragen
        loan.Status = "returned";
        loan.ReturnDate = DateOnly.FromDateTime(DateTime.Today);

        // Exemplar-Status wieder auf "available" (verfügbar) setzen
        if (payload.TryGetProperty("specimenId", out var specElem) && specElem.ValueKind != JsonValueKind.Null)
        {
            int specimenId = specElem.GetInt32();
            var specimen = await _db.Specimens.FindAsync(specimenId);
            if (specimen != null)
            {
                specimen.Status = "available";
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "Exemplar erfolgreich zurückgegeben." });
    }
}