using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Zoolog.Models;

namespace Zoolog.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecimenController : ControllerBase
{
    private readonly Group6DbContext _db;

    public SpecimenController(Group6DbContext db)
    {
        _db = db;
    }

    // Public users may only see specimens from public collections.
    // Logged-in users may see public specimens plus specimens they added themselves.
    // GET /api/specimen
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isLoggedIn = int.TryParse(userIdText, out var userId);

        var specimens = await _db.Specimens
            .Where(specimen =>
                specimen.Collection.IsPublic ||
                (isLoggedIn && specimen.AddedBy == userId))
            .Select(specimen => new
            {
                specimen.Id,
                specimen.Name,
                specimen.Description,
                specimen.DateCollected,
                specimen.Status,
                specimen.Size,
                specimen.PhotoPath,
                specimen.LocationId,
                specimen.TaxonomyId,
                specimen.CollectionId,
                specimen.AddedBy,
                specimen.CreatedAt
            })
            .ToListAsync();
        return Ok(specimens);
    }

    // Same visibility rule as GetAll:
    // public collection OR own added specimen.
    // GET /api/specimen/{id}
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isLoggedIn = int.TryParse(userIdText, out var userId);

        var specimen = await _db.Specimens
            .Where(specimen =>
                specimen.Id == id &&
                (specimen.Collection.IsPublic ||
                (isLoggedIn && specimen.AddedBy == userId)))
            .Select(specimen => new
            {
                specimen.Id,
                specimen.Name,
                specimen.Description,
                specimen.DateCollected,
                specimen.Status,
                specimen.Size,
                specimen.PhotoPath,
                specimen.LocationId,
                specimen.TaxonomyId,
                specimen.CollectionId,
                specimen.AddedBy,
                specimen.CreatedAt
            })
            .FirstOrDefaultAsync();
            
        if (specimen == null) return NotFound();
        return Ok(specimen);
    }

    // POST /api/specimen
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SpecimenRequest request)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var isPrivileged = User.IsInRole("admin") || User.IsInRole("moderator");
        
        var collection = await _db.Collections.FindAsync(request.CollectionId);
        if (collection is null)
        {
            return BadRequest(new { message = "Die Sammlung existiert nicht." });
        }

        if (!isPrivileged && collection.CreatedBy != userId)
        {
            return Forbid();
        }

        if (!await _db.Locations.AnyAsync(location => location.Id == request.LocationId))
        {
            return BadRequest(new { message = "Der Fundort existiert nicht." });
        }

        if (!await _db.Taxonomies.AnyAsync(taxonomy => taxonomy.Id == request.TaxonomyId))
        {
            return BadRequest(new { message = "Die Taxonomie existiert nicht." });
        }

        var specimen = new Specimen
        {
            Name = request.Name,
            Description = request.Description,
            DateCollected = request.DateCollected ?? DateOnly.FromDateTime(DateTime.Now),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "available" : request.Status,
            Size = request.Size,
            PhotoPath = request.PhotoPath,
            LocationId = request.LocationId,
            TaxonomyId = request.TaxonomyId,
            CollectionId = request.CollectionId,
            AddedBy = userId,
            CreatedAt = DateTime.Now
        };

        _db.Specimens.Add(specimen);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = specimen.Id }, ToResponse(specimen));
    }

    // PUT /api/specimen/{id}
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SpecimenRequest request)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var specimen = await _db.Specimens.FindAsync(id);
        if (specimen is null)
        {
            return NotFound();
        }

        var isPrivileged = User.IsInRole("admin") || User.IsInRole("moderator");

        if (!isPrivileged && specimen.AddedBy != userId)
        {
            return Forbid();
        }

        var collection = await _db.Collections.FindAsync(request.CollectionId);
        if (collection is null)
        {
            return BadRequest(new { message = "Die Sammlung existiert nicht." });
        }

        if (!isPrivileged && collection.CreatedBy != userId)
        {
            return Forbid();
        }

        if (!await _db.Locations.AnyAsync(location => location.Id == request.LocationId))
        {
            return BadRequest(new { message = "Der Fundort existiert nicht." });
        }

        if (!await _db.Taxonomies.AnyAsync(taxonomy => taxonomy.Id == request.TaxonomyId))
        {
            return BadRequest(new { message = "Die Taxonomie existiert nicht." });
        }

        specimen.Name = request.Name;
        specimen.Description = request.Description;
        specimen.DateCollected = request.DateCollected ?? specimen.DateCollected;
        specimen.Status = string.IsNullOrWhiteSpace(request.Status) ? specimen.Status : request.Status;
        specimen.Size = request.Size;
        specimen.PhotoPath = request.PhotoPath;
        specimen.LocationId = request.LocationId;
        specimen.TaxonomyId = request.TaxonomyId;
        specimen.CollectionId = request.CollectionId;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /api/specimen/{id}
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var specimen = await _db.Specimens.FindAsync(id);
        if (specimen is null)
        {
            return NotFound();
        }

        var isPrivileged = User.IsInRole("admin") || User.IsInRole("moderator");

        if (!isPrivileged && specimen.AddedBy != userId)
        {
            return Forbid();
        }

        _db.Specimens.Remove(specimen);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpPost("import-csv/{collectionId}")]
    public async Task<IActionResult> ImportCsv(int collectionId,[FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Keine Datei hochgeladen." });

        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var collection = await _db.Collections.FindAsync(collectionId);
        if (collection == null)
            return NotFound(new { message = "Sammlung nicht gefunden." });

        var specimensToCreate = new List<Specimen>();

        try
        {
            Console.WriteLine($"[CSV-DEBUG] Starte Import für CollectionId: {collectionId}");
            Console.WriteLine($"[CSV-DEBUG] Dateiname: {file.FileName}, Größe: {file.Length} Bytes");

            using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                // Header-Zeile lesen und loggen
                var headerLine = await reader.ReadLineAsync();
                Console.WriteLine($"[CSV-DEBUG] Header gelesen: '{headerLine}'");

                int zeilenZaehler = 1;
                string? line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    zeilenZaehler++;
                    Console.WriteLine($"[CSV-DEBUG] Verarbeite Zeile {zeilenZaehler}: '{line}'");

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        Console.WriteLine($"[CSV-DEBUG] Zeile {zeilenZaehler} ist leer oder besteht nur aus Whitespaces. Überspringe.");
                        continue;
                    }

                    var parts = line.Split(';');
                    Console.WriteLine($"[CSV-DEBUG] Zeile {zeilenZaehler} in {parts.Length} Spalten gesplittet.");
                    
                    if (parts.Length < 9)
                    {
                        var msg = $"Fehler in Zeile {zeilenZaehler}: Die Zeile enthält nur {parts.Length} statt der 9 erforderlichen Spalten. Gefundenes Trennzeichen vielleicht kein Semikolon?";
                        Console.WriteLine($"[CSV-DEBUG] [VALIDIERUNGSFEHLER] {msg}");
                        return BadRequest(new { message = msg });
                    }

                    
                    var name = parts[0].Trim();
                    var speciesName = parts[1].Trim();
                    var locationName = parts[2].Trim();

                 
                    var kingdom = parts[3].Trim();
                    var phylum = parts[4].Trim();
                    var @class = parts[5].Trim(); 
                    var orders = parts[6].Trim();
                    var family = parts[7].Trim();
                    var genus = parts[8].Trim();

                    Console.WriteLine($"[CSV-DEBUG] Zeile {zeilenZaehler} extrahiert -> Name: {name}, Species: {speciesName}, Location: {locationName}");

                    
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(speciesName) || string.IsNullOrEmpty(locationName) ||
                        string.IsNullOrEmpty(kingdom) || string.IsNullOrEmpty(phylum) || string.IsNullOrEmpty(@class) ||
                        string.IsNullOrEmpty(orders) || string.IsNullOrEmpty(family) || string.IsNullOrEmpty(genus))
                    {
                        var msg = $"Fehler in Zeile {zeilenZaehler}: Es wurden leere Werte nach dem Trimmen gefunden.";
                        Console.WriteLine($"[CSV-DEBUG] [VALIDIERUNGSFEHLER] {msg}");
                        return BadRequest(new { message = msg });
                    }

                    Console.WriteLine($"[CSV-DEBUG] Suche Location in DB: '{locationName}'");
                    var location = await _db.Locations.FirstOrDefaultAsync(l => l.Name.ToLower() == locationName.ToLower());
                    if (location == null)
                    {
                        Console.WriteLine($"[CSV-DEBUG] Location '{locationName}' nicht gefunden. Erstelle neu...");
                        location = new Location { Name = locationName };
                        _db.Locations.Add(location);
                        await _db.SaveChangesAsync(); 
                        Console.WriteLine($"[CSV-DEBUG] Neue Location mit ID {location.Id} gespeichert.");
                    }

                   
                    Console.WriteLine($"[CSV-DEBUG] Suche Taxonomy in DB (Species): '{speciesName}'");
                    var taxonomy = await _db.Taxonomies.FirstOrDefaultAsync(t => t.Species.ToLower() == speciesName.ToLower());
                    if (taxonomy == null)
                    {
                        Console.WriteLine($"[CSV-DEBUG] Taxonomy für '{speciesName}' nicht gefunden. Erstelle neu...");
                        taxonomy = new Taxonomy 
                        { 
                            Species = speciesName, 
                            Genus = genus,
                            Kingdom = kingdom,
                            Phylum = phylum,
                            Class = @class,
                            Orders = orders,
                            Family = family
                        };
                        _db.Taxonomies.Add(taxonomy);
                        await _db.SaveChangesAsync(); 
                        Console.WriteLine($"[CSV-DEBUG] Neue Taxonomy mit ID {taxonomy.Id} gespeichert.");
                    }

                    // 3. Exemplar vorbereiten
                    var specimen = new Specimen
                    {
                        Name = name,
                        CollectionId = collectionId,
                        LocationId = location.Id,
                        TaxonomyId = taxonomy.Id,
                        Status = "available",
                        DateCollected = DateOnly.FromDateTime(DateTime.Now),
                        AddedBy = userId,
                        CreatedAt = DateTime.Now
                    };

                    specimensToCreate.Add(specimen);
                    Console.WriteLine($"[CSV-DEBUG] Exemplar '{name}' erfolgreich für den Batch-Eintrag vorbereitet.");
                }
            }

            Console.WriteLine($"[CSV-DEBUG] Schleife beendet. Anzahl zu speichernder Exemplare: {specimensToCreate.Count}");

            if (specimensToCreate.Count > 0)
            {
                Console.WriteLine("[CSV-DEBUG] Führe AddRange und SaveChangesAsync für Exemplare aus...");
                _db.Specimens.AddRange(specimensToCreate);
                await _db.SaveChangesAsync();
                Console.WriteLine("[CSV-DEBUG] Alle Exemplare erfolgreich in die Datenbank geschrieben!");
            }

            return Ok(new { message = $"{specimensToCreate.Count} Exemplare erfolgreich importiert." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CSV-DEBUG] [KRITISCHER FEHLER] Exception geworfen: {ex.Message}");
            Console.WriteLine($"[CSV-DEBUG] StackTrace: {ex.StackTrace}");
            
           
            return BadRequest(new { 
                message = "Fehler beim Verarbeiten der CSV auf Serverebene.", 
                error = ex.Message,
                detail = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpGet("export-csv/{collectionId}")]
    public async Task<IActionResult> ExportCsv(int collectionId)
    {
       
        var specimens = await _db.Specimens
            .Include(s => s.Location)
            .Include(s => s.Taxonomy)
            .Where(s => s.CollectionId == collectionId)
            .ToListAsync();

        var sb = new System.Text.StringBuilder();

        
        sb.AppendLine("Name;Species;Location;Kingdom;Phylum;Class;Orders;Family;Genus");

        foreach (var s in specimens)
        {
           
            var locationName = s.Location?.Name ?? "";
            var species = s.Taxonomy?.Species ?? "";
            var kingdom = s.Taxonomy?.Kingdom ?? "";
            var phylum = s.Taxonomy?.Phylum ?? "";
            var @class = s.Taxonomy?.Class ?? "";
            var orders = s.Taxonomy?.Orders ?? "";
            var family = s.Taxonomy?.Family ?? "";
            var genus = s.Taxonomy?.Genus ?? "";

            
            sb.AppendLine($"{s.Name};{species};{locationName};{kingdom};{phylum};{@class};{orders};{family};{genus}");
        }

       
        var csvBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"sammlung_{collectionId}_export_{DateTime.Now:yyyyMMdd}.csv";

        return File(csvBytes, "text/csv", fileName);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdText, out userId);
    }

    private static object ToResponse(Specimen specimen)
    {
        return new
        {
            specimen.Id,
            specimen.Name,
            specimen.Description,
            specimen.DateCollected,
            specimen.Status,
            specimen.Size,
            specimen.PhotoPath,
            specimen.LocationId,
            specimen.TaxonomyId,
            specimen.CollectionId,
            specimen.AddedBy,
            specimen.CreatedAt
        };
    }
}

public class SpecimenRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly? DateCollected { get; set; }

    [StringLength(30)]
    [RegularExpression("^(available|on loan|lost|destroyed)$", ErrorMessage = "Status muss einer der folgenden Werte sein: available, on loan, lost, destroyed.")]
    public string? Status { get; set; }

    [StringLength(100)]
    public string? Size { get; set; }

    [StringLength(500)]
    public string? PhotoPath { get; set; }

    [Range(1, int.MaxValue)]
    public int LocationId { get; set; }

    [Range(1, int.MaxValue)]
    public int TaxonomyId { get; set; }

    [Range(1, int.MaxValue)]
    public int CollectionId { get; set; }
}