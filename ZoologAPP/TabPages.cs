//08.07.2026 Alexander Stojek: Die Seiten der unteren TabBar.
//08.07.2026 Alexander Stojek (Struktur-Umbau): Auf 5 Tabs umgestellt
// (Start, Sammlungen, Suche, Fundorte, Mehr). Exponate leben jetzt INNERHALB einer Sammlung,
// Konto + Leihgaben liegen im Tab "Mehr". Jede Seite erbt von BasePage und legt nur Titel + Ansicht fest.

namespace ZoologAPP;

public class HomePage : BasePage
{
	//08.07.2026 Alexander Stojek (Struktur-Umbau): Titel von "Home" auf "Start" geändert.
	public HomePage() { Title = "Start"; }
	protected override void OnAppearing() { view = "home"; base.OnAppearing(); }
}

public class CollectionsPage : BasePage
{
	public CollectionsPage() { Title = "Sammlungen"; }
	protected override void OnAppearing() { view = "collections"; base.OnAppearing(); }
}

//08.07.2026 Alexander Stojek (Struktur-Umbau): neuer Tab "Suche".
public class SearchPage : BasePage
{
	public SearchPage() { Title = "Suche"; }
	protected override void OnAppearing() { view = "search"; base.OnAppearing(); }
}

public class LocationsPage : BasePage
{
	//08.07.2026 Alexander Stojek (Struktur-Umbau): Titel von "Standorte" auf "Fundorte" geändert.
	public LocationsPage() { Title = "Fundorte"; }
	protected override void OnAppearing() { view = "locations"; base.OnAppearing(); }
}

//08.07.2026 Alexander Stojek (Struktur-Umbau): neuer Sammel-Tab "Mehr" (Konto, Leihgaben, später Einstellungen).
// Nicht angemeldet -> zeigt direkt den Login; angemeldet -> zeigt das Menü.
public class MorePage : BasePage
{
	public MorePage() { Title = "Mehr"; }
	protected override void OnAppearing() { view = auth.IsLoggedIn ? "more" : "auth"; base.OnAppearing(); }
}

//08.07.2026 Alexander Stojek (Struktur-Umbau): Diese drei Tab-Seiten werden nicht mehr als eigene Tabs verwendet
// (Exponate stecken jetzt in den Sammlungen; Leihgaben + Konto liegen im Tab "Mehr"). Bewusst nur auskommentiert, nicht gelöscht.
/*
public class ObjectsPage : BasePage
{
	public ObjectsPage() { Title = "Exponate"; }
	protected override void OnAppearing() { view = "objects"; base.OnAppearing(); }
}

public class LoansPage : BasePage
{
	public LoansPage() { Title = "Leihgaben"; }
	protected override void OnAppearing() { view = "loans"; base.OnAppearing(); }
}

public class AccountPage : BasePage
{
	public AccountPage() { Title = "Konto"; }
	protected override void OnAppearing() { view = "auth"; base.OnAppearing(); }
}
*/
