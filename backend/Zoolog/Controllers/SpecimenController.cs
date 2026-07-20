using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Zoolog.Models;

namespace Zoolog.Controllers;

[ApiController]
[Route("api/specimen")]
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
        
        // Prüfen, ob der User Admin oder Moderator ist (falls bei dir in den Claims hinterlegt)
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        var isAdminOrMod = userRole == "admin" || userRole == "moderator";

        var specimens = await _db.Specimens
            .Include(specimen => specimen.Taxonomy) // Wichtig: Taxonomie mitladen!
            .Include(specimen => specimen.Collection) // Wichtig: Collection für IsPublic mitladen!
            .Where(specimen =>
                // 1. Sammlung muss öffentlich sein ODER dem User gehören
                (specimen.Collection.IsPublic || (isLoggedIn && specimen.AddedBy == userId)) &&
                
                // 2. ENTWEDER Taxonomie ist validiert ODER der User ist Ersteller/Admin/Mod (dann unvalidierte anzeigen/ausgrauen)
                (specimen.Taxonomy.Validated || (isLoggedIn && (specimen.AddedBy == userId || isAdminOrMod)))
            )
            .Select(specimen => new
            {
                specimen.Id,
                specimen.Name,
                specimen.Description,
                specimen.DateCollected,
                specimen.Status,
                specimen.Size,
                specimen.Weight,
                specimen.BirthYear,
                specimen.PhotoPath,
                specimen.LocationId,
                specimen.TaxonomyId,
                specimen.CollectionId,
                specimen.AddedBy,
                specimen.CreatedAt,
                // NEU: Direkt mitgeben, ob die Taxonomie validiert ist (fürs Frontend zum Ausgrauen)
                createdBy = specimen.Collection.CreatedBy,
                TaxonomyValidated = specimen.Taxonomy != null && specimen.Taxonomy.Validated
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
                specimen.Weight,
                specimen.BirthYear,
                specimen.PhotoPath,
                specimen.LocationId,
                
                // --- NEU: Hier greifen wir direkt auf die verknüpfte Location zu ---
                Latitude = specimen.Location != null ? specimen.Location.Latitude : (decimal?)null,
                Longitude = specimen.Location != null ? specimen.Location.Longitude : (decimal?)null,
                // ------------------------------------------------------------------

                specimen.TaxonomyId,
                specimen.CollectionId,
                specimen.AddedBy,
                specimen.CreatedAt
            })
            .FirstOrDefaultAsync();
            
        if (specimen == null) return NotFound();
        return Ok(specimen);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] SpecimenRequest request, IFormFile? imageFile)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("[CREATE-DEBUG] POST /api/specimen (inkl. Taxonomy Logik)");
        Console.WriteLine($"[DEBUG-CHECK] Empfangenes Gewicht: {request.Weight}, Geburtsjahr: {request.BirthYear}");

        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var collection = await _db.Collections.FindAsync(request.CollectionId);
        if (collection is null) return BadRequest(new { message = "Die Sammlung existiert nicht." });

        if (!User.IsInRole("admin") && !User.IsInRole("moderator") && collection.CreatedBy != userId)
            return Forbid();

        // 1. Taxonomie-Logik (NEU)
        int finalTaxonomyId;
        if (request.TaxonomyId.HasValue && request.TaxonomyId.Value > 0)
        {
            if (!await _db.Taxonomies.AnyAsync(t => t.Id == request.TaxonomyId.Value))
                return BadRequest(new { message = "Die gewählte Taxonomie existiert nicht." });
            finalTaxonomyId = request.TaxonomyId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(request.Genus) && !string.IsNullOrWhiteSpace(request.Species))
        {
            var newTaxonomy = new Taxonomy
            {
                Kingdom = request.Kingdom ?? "Unbekannt",
                Phylum = request.Phylum ?? "Unbekannt",
                Class = request.Class ?? "Unbekannt",
                Orders = request.Orders ?? "Unbekannt",
                Family = request.Family ?? "Unbekannt",
                Genus = request.Genus,
                Species = request.Species,
                Validated = false
            };
            _db.Taxonomies.Add(newTaxonomy);
            await _db.SaveChangesAsync(); // Generiert die ID
            finalTaxonomyId = newTaxonomy.Id;
        }
        else
        {
            return BadRequest(new { message = "TaxonomyId oder manuelle Gattung/Art erforderlich." });
        }

        // 2. Location Logik
        int finalLocationId = 0;
        if (request.LocationId.HasValue && request.LocationId.Value > 0)
        {
            if (!await _db.Locations.AnyAsync(l => l.Id == request.LocationId.Value))
                return BadRequest(new { message = "Fundort existiert nicht." });
            finalLocationId = request.LocationId.Value;
        }
        else if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var newLocation = new Location { 
                Name = $"Fundort ({request.Latitude.Value:F4}, {request.Longitude.Value:F4})", 
                Latitude = request.Latitude.Value, Longitude = request.Longitude.Value 
            };
            _db.Locations.Add(newLocation);
            await _db.SaveChangesAsync();
            finalLocationId = newLocation.Id;
        }
        else return BadRequest(new { message = "LocationId oder Koordinaten erforderlich." });

        // 3. Foto-Upload
        string? savedPhotoPath = null;
        if (imageFile != null && imageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine("wwwroot", "images", "specimens");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            savedPhotoPath = "/images/specimens/" + fileName;
        }

        // 4. Specimen speichern
        try
        {
            var specimen = new Specimen
            {
                Name = request.Name,
                Description = request.Description,
                DateCollected = request.DateCollected ?? DateOnly.FromDateTime(DateTime.Now),
                Status = string.IsNullOrWhiteSpace(request.Status) ? "available" : request.Status,
                Size = request.Size,
                Weight = request.Weight,       // <-- HIER ERGÄNZEN
                BirthYear = request.BirthYear,
                PhotoPath = savedPhotoPath,
                LocationId = finalLocationId,
                TaxonomyId = finalTaxonomyId, // Hier die finale ID
                CollectionId = request.CollectionId,
                AddedBy = userId,
                CreatedAt = DateTime.Now
            };

            _db.Specimens.Add(specimen);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = specimen.Id }, ToResponse(specimen));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Fehler beim Speichern.", error = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] SpecimenRequest request, IFormFile? imageFile)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine($"[UPDATE-DEBUG] PUT /api/specimen/{id} aufgerufen!");

        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var specimen = await _db.Specimens.FindAsync(id);
        if (specimen is null) return NotFound();

        var isPrivileged = User.IsInRole("admin") || User.IsInRole("moderator");
        if (!isPrivileged && specimen.AddedBy != userId) return Forbid();

        var collection = await _db.Collections.FindAsync(request.CollectionId);
        if (collection is null) return BadRequest(new { message = "Die Sammlung existiert nicht." });

        if (!isPrivileged && collection.CreatedBy != userId) return Forbid();

        // 1. Bild-Logik (unverändert)
        if (imageFile != null && imageFile.Length > 0)
        {
            if (!string.IsNullOrEmpty(specimen.PhotoPath))
            {
                var oldPath = Path.Combine("wwwroot", specimen.PhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
            var uploadsFolder = Path.Combine("wwwroot", "images", "specimens");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            specimen.PhotoPath = "/images/specimens/" + fileName;
        }

        // 2. Location-Logik (unverändert)
        var targetLocationId = request.LocationId ?? specimen.LocationId;
        if (!await _db.Locations.AnyAsync(l => l.Id == targetLocationId))
            return BadRequest(new { message = "Der Fundort existiert nicht." });

        // 3. Taxonomie-Logik (Integration der manuellen Eingabe)
        int finalTaxonomyId = specimen.TaxonomyId; // Default: alte ID behalten

        if (request.TaxonomyId.HasValue && request.TaxonomyId.Value > 0)
        {
            // Modus: Bestehende ID gewählt
            if (!await _db.Taxonomies.AnyAsync(t => t.Id == request.TaxonomyId.Value))
                return BadRequest(new { message = "Die gewählte Taxonomie existiert nicht." });
            finalTaxonomyId = request.TaxonomyId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(request.Genus) && !string.IsNullOrWhiteSpace(request.Species))
        {
            // Modus: Neue Taxonomie manuell angelegt
            var newTaxonomy = new Taxonomy
            {
                Kingdom = request.Kingdom ?? "Unbekannt",
                Phylum = request.Phylum ?? "Unbekannt",
                Class = request.Class ?? "Unbekannt",
                Orders = request.Orders ?? "Unbekannt",
                Family = request.Family ?? "Unbekannt",
                Genus = request.Genus,
                Species = request.Species,
                Validated = false
            };
            _db.Taxonomies.Add(newTaxonomy);
            await _db.SaveChangesAsync();
            finalTaxonomyId = newTaxonomy.Id;
        }
        else if (request.TaxonomyId == null && (string.IsNullOrWhiteSpace(request.Genus)))
        {
            return BadRequest(new { message = "Taxonomie oder Gattung/Art ist erforderlich." });
        }

        // 4. Text-Daten aktualisieren
        try
        {
            specimen.Name = request.Name;
            specimen.Description = request.Description;
            specimen.DateCollected = request.DateCollected ?? specimen.DateCollected;
            specimen.Status = string.IsNullOrWhiteSpace(request.Status) ? specimen.Status : request.Status;
            specimen.Size = request.Size;
            specimen.Weight = request.Weight;     // <-- HIER ERGÄNZEN
            specimen.BirthYear = request.BirthYear;
            specimen.LocationId = targetLocationId;
            specimen.TaxonomyId = finalTaxonomyId;
            specimen.CollectionId = request.CollectionId;

            await _db.SaveChangesAsync();
            
            Console.WriteLine("[UPDATE-DEBUG] Update erfolgreich!");
            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UPDATE-DEBUG] Fehler: {ex.Message}");
            return StatusCode(500, new { message = "Fehler beim Updaten des Objekts.", error = ex.Message });
        }
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

        if (!string.IsNullOrEmpty(specimen.PhotoPath))
        {
            var filePath = Path.Combine("wwwroot", specimen.PhotoPath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
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
    public async Task<IActionResult> ImportCsv(int collectionId, [FromForm] IFormFile file)
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
                        continue;
                    }

                    var parts = line.Split(';');
                    Console.WriteLine($"[CSV-DEBUG] Zeile {zeilenZaehler} in {parts.Length} Spalten gesplittet.");
                    
                    // Erwartet 16 Spalten: 9 Basis + Description, Status, Size, Weight, BirthYear, Latitude, Longitude
                    if (parts.Length < 16)
                    {
                        var msg = $"Fehler in Zeile {zeilenZaehler}: Die Zeile enthält nur {parts.Length} statt der 16 erforderlichen Spalten.";
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
                    
                    var description = parts[9].Trim();
                    var status = parts[10].Trim();
                    var size = parts[11].Trim();
                    
                    var weightStr = parts[12].Trim().Replace(',', '.');
                    var birthYearStr = parts[13].Trim();
                    
                    // Latitude und Longitude als String einlesen
                    var latStr = parts[14].Trim().Replace(',', '.');
                    var lonStr = parts[15].Trim().Replace(',', '.');

                    Console.WriteLine($"[CSV-DEBUG] Zeile {zeilenZaehler} extrahiert -> Name: {name}, Species: {speciesName}, Location: {locationName}");

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(speciesName) || string.IsNullOrEmpty(locationName) ||
                        string.IsNullOrEmpty(kingdom) || string.IsNullOrEmpty(phylum) || string.IsNullOrEmpty(@class) ||
                        string.IsNullOrEmpty(orders) || string.IsNullOrEmpty(family) || string.IsNullOrEmpty(genus))
                    {
                        var msg = $"Fehler in Zeile {zeilenZaehler}: Es wurden Pflichtwerte leer gelassen.";
                        Console.WriteLine($"[CSV-DEBUG] [VALIDIERUNGSFEHLER] {msg}");
                        return BadRequest(new { message = msg });
                    }

                    // Optionales parsen für Weight (decimal?)
                    decimal? weight = null;
                    if (!string.IsNullOrEmpty(weightStr) && decimal.TryParse(weightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedWeight))
                    {
                        weight = parsedWeight;
                    }

                    // Optionales parsen für BirthYear (int?)
                    int? birthYear = null;
                    if (!string.IsNullOrEmpty(birthYearStr) && int.TryParse(birthYearStr, out var parsedYear))
                    {
                        birthYear = parsedYear;
                    }

                    // Optionales parsen für Latitude (decimal?)
                    decimal? latitude = null;
                    if (!string.IsNullOrEmpty(latStr) && decimal.TryParse(latStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedLat))
                    {
                        latitude = parsedLat;
                    }

                    // Optionales parsen für Longitude (decimal?)
                    decimal? longitude = null;
                    if (!string.IsNullOrEmpty(lonStr) && decimal.TryParse(lonStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedLon))
                    {
                        longitude = parsedLon;
                    }

                    // Location suchen oder erstellen (inklusive Koordinaten)
                    var location = await _db.Locations.FirstOrDefaultAsync(l => l.Name.ToLower() == locationName.ToLower());
                    if (location == null)
                    {
                        location = new Location 
                        { 
                            Name = locationName,
                            Latitude = latitude ?? 0,
                            Longitude = longitude ?? 0
                        };
                        _db.Locations.Add(location);
                        await _db.SaveChangesAsync(); 
                    }
                    else if ((latitude.HasValue || longitude.HasValue) && (location.Latitude == 0 && location.Longitude == 0))
                    {
                        if (latitude.HasValue) location.Latitude = latitude.Value;
                        if (longitude.HasValue) location.Longitude = longitude.Value;
                        await _db.SaveChangesAsync();
                    }

                    var taxonomy = await _db.Taxonomies.FirstOrDefaultAsync(t => t.Species.ToLower() == speciesName.ToLower());
                    if (taxonomy == null)
                    {
                        taxonomy = new Taxonomy 
                        { 
                            Species = speciesName, 
                            Genus = genus,
                            Kingdom = kingdom,
                            Phylum = phylum,
                            Class = @class,
                            Orders = orders,
                            Family = family,
                            Validated = false
                        };
                        _db.Taxonomies.Add(taxonomy);
                        await _db.SaveChangesAsync(); 
                    }

                    var specimen = new Specimen
                    {
                        Name = name,
                        CollectionId = collectionId,
                        LocationId = location.Id,
                        TaxonomyId = taxonomy.Id,
                        Description = string.IsNullOrEmpty(description) ? null : description,
                        Status = string.IsNullOrWhiteSpace(status) ? "available" : status,
                        Size = string.IsNullOrEmpty(size) ? null : size,
                        Weight = weight,
                        BirthYear = birthYear,
                        DateCollected = DateOnly.FromDateTime(DateTime.Now),
                        AddedBy = userId,
                        CreatedAt = DateTime.Now
                    };

                    

                    specimensToCreate.Add(specimen);

                       
                }
            }


            if (specimensToCreate.Count > 0)
            {
                _db.Specimens.AddRange(specimensToCreate);
                await _db.SaveChangesAsync();
            }

            return Ok(new { message = $"{specimensToCreate.Count} Exemplare erfolgreich importiert." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CSV-DEBUG] [KRITISCHER FEHLER] Exception: {ex.Message}");
            return BadRequest(new { 
                message = "Fehler beim Verarbeiten der CSV auf Serverebene.", 
                error = ex.Message,
                detail = ex.InnerException?.Message
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

        // Header inklusive Latitude und Longitude
        sb.AppendLine("Name;Species;Location;Kingdom;Phylum;Class;Orders;Family;Genus;Description;Status;Size;Weight;BirthYear;Latitude;Longitude");

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
            
            var description = (s.Description ?? "").Replace(";", ",");
            var status = s.Status ?? "";
            var size = (s.Size ?? "").Replace(";", ",");
            
            var weight = s.Weight.HasValue ? s.Weight.Value.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : "";
            var birthYear = s.BirthYear.HasValue ? s.BirthYear.Value.ToString() : "";
            
            // Latitude und Longitude für den Export aufbereiten (korrekt für decimal-Typen)
            var latitude = s.Location != null ? s.Location.Latitude.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : "";
            var longitude = s.Location != null ? s.Location.Longitude.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : "";

            sb.AppendLine($"{s.Name};{species};{locationName};{kingdom};{phylum};{@class};{orders};{family};{genus};{description};{status};{size};{weight};{birthYear};{latitude};{longitude}");
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
            specimen.Weight,       // <-- HIER ERGÄNZEN
            specimen.BirthYear,
            specimen.PhotoPath,
            specimen.TaxonomyId,
            specimen.CollectionId,
            specimen.AddedBy,
            specimen.CreatedAt,
            LocationId = specimen.LocationId,
            Latitude = specimen.Location?.Latitude,
            Longitude = specimen.Location?.Longitude
        };
    }

    [AllowAnonymous]
    [HttpGet("search-public")]
    public async Task<IActionResult> SearchPublicSpecimens(
        [FromQuery] string? query,
        [FromQuery] string? className,
        [FromQuery] string? species,
        [FromQuery] string? genus,
        [FromQuery] string? family,
        [FromQuery] string? order,
        [FromQuery] decimal? minWeight,
        [FromQuery] decimal? maxWeight,
        [FromQuery] decimal? minSize,
        [FromQuery] decimal? maxSize,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] double? radius)
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isLoggedIn = int.TryParse(userIdText, out var userId);
        
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        var isAdminOrMod = userRole == "admin" || userRole == "moderator";

        var specimensQuery = _db.Specimens
            .Include(specimen => specimen.Taxonomy)
            .Include(specimen => specimen.Collection)
            .Include(specimen => specimen.Location)
            .Where(specimen =>
                (specimen.Collection.IsPublic || (isLoggedIn && specimen.AddedBy == userId)) &&
                (specimen.Taxonomy.Validated || (isLoggedIn && (specimen.AddedBy == userId || isAdminOrMod)))
            )
            .AsQueryable();

        // 1. Freitextsuche
        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLower();
            specimensQuery = specimensQuery.Where(s => 
                (s.Name != null && EF.Functions.Like(s.Name, $"%{query}%")) || 
                (s.Description != null && EF.Functions.Like(s.Description, $"%{query}%")) ||
                (s.Taxonomy != null && (
                    (s.Taxonomy.Species != null && EF.Functions.Like(s.Taxonomy.Species, $"%{query}%")) ||
                    (s.Taxonomy.Genus != null && EF.Functions.Like(s.Taxonomy.Genus, $"%{query}%")) ||
                    (s.Taxonomy.Family != null && EF.Functions.Like(s.Taxonomy.Family, $"%{query}%")) ||
                    (s.Taxonomy.Orders != null && EF.Functions.Like(s.Taxonomy.Orders, $"%{query}%")) ||
                    (s.Taxonomy.Class != null && EF.Functions.Like(s.Taxonomy.Class, $"%{query}%"))
                ))
            );
        }

        // 2. Taxonomie-Filter (Klasse, Spezies, Genus, etc.)
        if (!string.IsNullOrWhiteSpace(className))
            specimensQuery = specimensQuery.Where(s => s.Taxonomy != null && s.Taxonomy.Class != null && s.Taxonomy.Class.ToLower() == className.ToLower());

        if (!string.IsNullOrWhiteSpace(species))
            specimensQuery = specimensQuery.Where(s => s.Taxonomy != null && s.Taxonomy.Species != null && s.Taxonomy.Species.ToLower().Contains(species.ToLower()));

        if (!string.IsNullOrWhiteSpace(genus))
            specimensQuery = specimensQuery.Where(s => s.Taxonomy != null && s.Taxonomy.Genus != null && s.Taxonomy.Genus.ToLower().Contains(genus.ToLower()));

        if (!string.IsNullOrWhiteSpace(family))
            specimensQuery = specimensQuery.Where(s => s.Taxonomy != null && s.Taxonomy.Family != null && s.Taxonomy.Family.ToLower().Contains(family.ToLower()));

        if (!string.IsNullOrWhiteSpace(order))
            specimensQuery = specimensQuery.Where(s => s.Taxonomy != null && s.Taxonomy.Orders != null && s.Taxonomy.Orders.ToLower().Contains(order.ToLower()));

        // 3. Gewichts-Intervalle direkt im DB-Query
        if (minWeight.HasValue)
            specimensQuery = specimensQuery.Where(s => s.Weight.HasValue && s.Weight.Value >= minWeight.Value);
        
        if (maxWeight.HasValue)
            specimensQuery = specimensQuery.Where(s => s.Weight.HasValue && s.Weight.Value <= maxWeight.Value);

        // ERST HIER wird die Datenbank abgefragt – nach allen Text- und Taxonomie-Filtern!
        var specimensList = await specimensQuery.ToListAsync();

        // 4. Größen-Filter (string zu decimal Konvertierung im Arbeitsspeicher)
        if (minSize.HasValue)
        {
            specimensList = specimensList.Where(s => 
            {
                if (string.IsNullOrWhiteSpace(s.Size)) return false;
                // Entferne alles außer Ziffern und Punkten/Kommas
                var cleanSizeStr = new string(s.Size.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
                                    .Replace(',', '.');
                
                return decimal.TryParse(cleanSizeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedSize) 
                    && parsedSize >= minSize.Value;
            }).ToList();
        }

        if (maxSize.HasValue)
        {
            specimensList = specimensList.Where(s => 
            {
                if (string.IsNullOrWhiteSpace(s.Size)) return false;
                var cleanSizeStr = new string(s.Size.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
                                    .Replace(',', '.');
                
                return decimal.TryParse(cleanSizeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedSize) 
                    && parsedSize <= maxSize.Value;
            }).ToList();
        }

        // 5. Geografischer Radius-Filter
        if (lat.HasValue && lng.HasValue && radius.HasValue)
        {
            specimensList = specimensList.Where(s => 
            {
                var specimenLat = s.Location?.Latitude;
                var specimenLng = s.Location?.Longitude;

                if (!specimenLat.HasValue || !specimenLng.HasValue) return false;

                return CalculateDistance(lat.Value, lng.Value, (double)specimenLat.Value, (double)specimenLng.Value) <= radius.Value;
            }).ToList();
        }

        // 6. Projektion
        var result = specimensList.Select(specimen => new
        {
            specimen.Id,
            specimen.Name,
            specimen.Description,
            specimen.DateCollected,
            specimen.Status,
            specimen.Size,
            specimen.Weight,
            specimen.BirthYear,
            specimen.PhotoPath,
            specimen.LocationId,
            specimen.TaxonomyId,
            specimen.CollectionId,
            specimen.AddedBy,
            specimen.CreatedAt,
            createdBy = specimen.Collection.CreatedBy,
            TaxonomyValidated = specimen.Taxonomy != null && specimen.Taxonomy.Validated,
            Class = specimen.Taxonomy?.Class,
            Species = specimen.Taxonomy?.Species,
            Genus = specimen.Taxonomy?.Genus,
            Family = specimen.Taxonomy?.Family,
            Orders = specimen.Taxonomy?.Orders,
            Latitude = specimen.Location?.Latitude,
            Longitude = specimen.Location?.Longitude
        }).ToList();

        return Ok(result);
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return 6371 * c;
    }

    private double ToRadians(double angle) => angle * Math.PI / 180.0;
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

    public int? LocationId { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public decimal? Weight { get; set; }
    public int? BirthYear { get; set; }

    public int? TaxonomyId { get; set; }

    

    [Range(1, int.MaxValue)]
    public int CollectionId { get; set; }

    [StringLength(100)]
    public string? Kingdom { get; set; }
    [StringLength(100)]
    public string? Phylum { get; set; }
    [StringLength(100)]
    public string? Class { get; set; }
    [StringLength(100)]
    public string? Orders { get; set; }
    [StringLength(100)]
    public string? Family { get; set; }
    [StringLength(100)]
    public string? Genus { get; set; }
    [StringLength(100)]
    public string? Species { get; set; }
}