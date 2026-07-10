//08.07.2026 Alexander Stojek: Bauteil für die Verbindung zum Backend (die "Telefonleitung").
// Meldet sich beim Server an (Login), merkt sich den Token und holt Daten (Sammlungen, Exponate, Taxonomie, Fundorte).
// Backend-Adresse: im Android-Emulator ist 10.0.2.2 = "der PC, auf dem der Emulator läuft"; das Backend hört auf Port 5227.

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Devices;

namespace ZoologAPP.Services;

public class ApiService
{
	//08.07.2026 Alexander Stojek: Server-Adresse je nach Plattform.
	// Android-Emulator: 10.0.2.2 = "der PC, auf dem der Emulator läuft". Sonst (Windows): localhost.
	// Für ein ECHTES Handy müsste hier die WLAN-IP des PCs stehen.
	static readonly string BaseUrl =
		DeviceInfo.Platform == DevicePlatform.Android
			? "http://10.0.2.2:5227/api"
			: "http://localhost:5227/api";

	static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(20) };
	static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

	public string? Token { get; private set; }
	public ApiUser? CurrentUser { get; private set; }
	public bool IsLoggedIn => Token is not null;

	//08.07.2026 Alexander Stojek: Anmeldung beim Backend. Gibt (ok, Fehlertext) zurück, damit man in der UI SIEHT, was schiefging
	// (vorher wurde ein Verbindungsfehler nirgends aufgefangen -> beim Klick auf "Einloggen" passierte optisch nichts).
	public async Task<(bool ok, string? error)> LoginAsync(string usernameOrEmail, string password)
	{
		try
		{
			var body = new { usernameOrEmail, password };
			var response = await http.PostAsJsonAsync($"{BaseUrl}/auth/login", body);
			if (!response.IsSuccessStatusCode)
				return (false, $"Server antwortete mit Fehler {(int)response.StatusCode} (Benutzername/Passwort prüfen).");

			var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
			if (result?.Token is null)
				return (false, "Unerwartete Antwort vom Server (kein Token erhalten).");

			Token = result.Token;
			CurrentUser = result.User;
			// Ab jetzt hängt die App den Login-Ausweis (Token) an jede Anfrage.
			http.DefaultRequestHeaders.Authorization =
				new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
			return (true, null);
		}
		catch (TaskCanceledException)
		{
			return (false, $"Zeitüberschreitung – Server unter {BaseUrl} nicht erreichbar. Läuft „dotnet run“? Ist das VPN an?");
		}
		catch (HttpRequestException ex)
		{
			return (false, $"Keine Verbindung zu {BaseUrl}: {ex.Message}. Läuft „dotnet run“? Ist das VPN an?");
		}
	}

	//08.07.2026 Alexander Stojek (Backend): Registrierung über das Backend (POST /api/users, Rolle wird serverseitig auf "user" gesetzt).
	public async Task<(bool ok, string? error)> RegisterAsync(string username, string email, string password)
	{
		var body = new { username, email, pass = password };
		var response = await http.PostAsJsonAsync($"{BaseUrl}/users", body);
		if (response.IsSuccessStatusCode)
			return (true, null);

		try
		{
			var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOpts);
			return (false, error?.Message ?? "Registrierung fehlgeschlagen.");
		}
		catch
		{
			return (false, "Registrierung fehlgeschlagen.");
		}
	}

	//08.07.2026 Alexander Stojek (Backend): Abmelden – Token verwerfen.
	public void Logout()
	{
		Token = null;
		CurrentUser = null;
		http.DefaultRequestHeaders.Authorization = null;
	}

	// Holt Daten vom Backend (z. B. "collections", "specimen", "taxonomy", "location").
	public async Task<T> GetAsync<T>(string path)
	{
		var result = await http.GetFromJsonAsync<T>($"{BaseUrl}/{path}", JsonOpts);
		return result!;
	}
}

//08.07.2026 Alexander Stojek: Datenklassen, die zu den JSON-Antworten des Backends passen.
public class LoginResponse
{
	public string? Token { get; set; }
	public ApiUser? User { get; set; }
}

public class ErrorResponse
{
	public string? Message { get; set; }
}

public class ApiUser
{
	public int Id { get; set; }
	public string? Username { get; set; }
	public string? Email { get; set; }
	public string? Role { get; set; }
	public string? Status { get; set; }
}

public class ApiCollection
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? Description { get; set; }
	public bool IsPublic { get; set; }
	public int CreatedBy { get; set; }
}

public class ApiSpecimen
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? Description { get; set; }
	public string? DateCollected { get; set; }
	public string? Status { get; set; }
	public string? Size { get; set; }
	public string? PhotoPath { get; set; }
	public int LocationId { get; set; }
	public int TaxonomyId { get; set; }
	public int CollectionId { get; set; }
}

public class ApiTaxonomy
{
	public int Id { get; set; }
	public string? Kingdom { get; set; }
	public string? Phylum { get; set; }
	public string? Class { get; set; }
	public string? Orders { get; set; }
	public string? Family { get; set; }
	public string? Genus { get; set; }
	public string? Species { get; set; }
}

public class ApiLocation
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }
	public string? Country { get; set; }
	public string? Region { get; set; }
}
