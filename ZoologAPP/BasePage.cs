//08.07.2026 Alexander Stojek: Neue Basisseite für die untere TabBar.
// Diese Klasse enthält die gemeinsame Logik ALLER Tabs (Header, Render-Methoden, Helfer).
// Jeder Tab (siehe TabPages.cs) erbt von dieser Klasse und setzt nur seine eigene Ansicht (view).
// Die bisherige MainPage.xaml(.cs) bleibt unverändert erhalten, wird aber nicht mehr verwendet.

using ZoologAPP.Models;
using ZoologAPP.Services;
using Microsoft.Maui.Controls.Shapes;

namespace ZoologAPP;

public abstract class BasePage : ContentPage
{
	//08.07.2026 Alexander Stojek: Dienste sind static, damit ALLE Tabs denselben Login-Zustand teilen.
	protected static readonly AuthService auth = new();
	protected static readonly DataService data = new();

	//08.07.2026 Alexander Stojek: Scroll-Bereich + Inhalts-Stapel werden hier im Code aufgebaut
	// (früher standen sie in MainPage.xaml).
	protected readonly ScrollView RootScroll;
	protected readonly VerticalStackLayout RootStack;

	protected string view = "home";

	//08.07.2026 Alexander Stojek (Struktur-Umbau): Merkt sich, welche Sammlung im Detail geöffnet ist (für die Exponate-Ansicht innerhalb der Sammlung).
	protected string selectedCollectionId = "";

	//08.07.2026 Alexander Stojek (Feature B): Merkt sich, welches Exponat in der Detail-/Bearbeiten-Ansicht geöffnet ist.
	protected string selectedObjectId = "";

	//08.07.2026 Alexander Stojek (Feinschliff): Steuert, ob das Formular „Neue Sammlung“ ausgeklappt ist.
	//08.07.2026 Alexander Stojek (Feinschliff): static, damit der Schnellzugriff auf der Startseite das Formular im Sammlungen-Tab aufklappen kann.
	protected static bool showNewCollection = false;

	//08.07.2026 Alexander Stojek (Feinschliff): Steuert, ob das Formular „Neues Exponat“ (in der Sammlungs-Detailansicht) ausgeklappt ist.
	protected bool showNewObject = false;

	protected BasePage()
	{
		RootStack = new VerticalStackLayout { Padding = 18, Spacing = 16 };
		RootScroll = new ScrollView { Content = RootStack };
		Content = RootScroll;
		BackgroundColor = Color.FromArgb("#F4F7F5");

		//08.07.2026 Alexander Stojek (Feinschliff): Obere Shell-Titelleiste (zeigte „Start“ usw.) ausblenden – wir bauen unsere eigene Kopfzeile.
		Shell.SetNavBarIsVisible(this, false);
	}

	//08.07.2026 Alexander Stojek: Wird jedes Mal aufgerufen, wenn der Tab angezeigt wird -> Inhalt frisch aufbauen.
	protected override void OnAppearing()
	{
		base.OnAppearing();
		Render();
	}

	protected void Render()
	{
		RootStack.Clear();
		RootStack.Add(Header());

		if (view != "auth" && !auth.IsLoggedIn)
		{
			//08.07.2026 Alexander Stojek (Struktur-Umbau): Login liegt jetzt im Tab „Mehr“ – Hinweis entsprechend angepasst.
			//08.07.2026 Alexander Stojek (Feinschliff): Hinweis verweist jetzt auf den Login oben rechts. Alt:
			//RootStack.Add(Panel("Anmeldung nötig", "Bitte melde dich im Tab „Mehr“ an, um Zoolog zu nutzen."));
			RootStack.Add(Panel("Anmeldung nötig", "Bitte melde dich an – oben rechts auf „Login“ tippen."));
			return;
		}

		switch (view)
		{
			case "auth": RenderAuth(); break;
			case "collections": RenderCollections(); break;
			//08.07.2026 Alexander Stojek (Struktur-Umbau): drei neue Ansichten (Sammlungs-Detail, Suche, Mehr-Menü).
			case "collectionDetail": RenderCollectionDetail(); break;
			case "search": RenderSearch(); break;
			case "more": RenderMore(); break;
			//08.07.2026 Alexander Stojek (Feature B): Einstellungen + Exponat-Detail/Bearbeiten.
			case "settings": RenderSettings(); break;
			case "objectDetail": RenderObjectDetail(); break;
			case "objects": RenderObjects(); break;
			case "loans": RenderLoans(); break;
			case "locations": RenderLocations(); break;
			default: RenderHome(); break;
		}
	}

	//08.07.2026 Alexander Stojek (Feinschliff): Alte Kopfzeile ersetzt. Alt:
	/*
	View Header()
	{
		var title = new Label { Text = "Zoolog", FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1B4332") };
		var sub = new Label { Text = auth.CurrentUser?.Username ?? "mobile sammlung", TextColor = Color.FromArgb("#6B7280") };
		return new VerticalStackLayout { Spacing = 2, Children = { title, sub } };
	}
	*/

	//08.07.2026 Alexander Stojek (Feinschliff): Neue Kopfzeile – links Logo + „Zoolog“, rechts Nutzername (angemeldet) bzw. „Login“-Button (führt zum Tab „Mehr“).
	View Header()
	{
		var logo = new Image { Source = "logo.png", WidthRequest = 30, HeightRequest = 30, VerticalOptions = LayoutOptions.Center };
		var brand = new Label { Text = "Zoolog", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1B4332"), VerticalOptions = LayoutOptions.Center };
		var left = new HorizontalStackLayout { Spacing = 8, Children = { logo, brand } };

		View right;
		if (auth.IsLoggedIn)
		{
			var user = new Label
			{
				Text = auth.CurrentUser!.Username,
				FontAttributes = FontAttributes.Bold,
				TextColor = Color.FromArgb("#1B4332"),
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.End
			};
			var tap = new TapGestureRecognizer();
			tap.Tapped += async (_, _) => await Shell.Current.GoToAsync("//more");
			user.GestureRecognizers.Add(tap);
			right = user;
		}
		else
		{
			var login = Button("Login", () => { _ = Shell.Current.GoToAsync("//more"); });
			login.HorizontalOptions = LayoutOptions.End;
			right = login;
		}

		var bar = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			}
		};
		bar.Add(left, 0, 0);
		bar.Add(right, 1, 0);
		return bar;
	}

	//08.07.2026 Alexander Stojek (Feinschliff): Startseite neu aufgebaut. Alt (nur „Dashboard“-Titel + Kacheln + populäre Sammlungen):
	/*
	void RenderHome()
	{
		var mine = auth.CurrentUser is null ? [] : data.GetMyCollections(auth.CurrentUser.Id);
		var myObjects = data.GetObjects().Where(o => mine.Any(c => c.Id == o.CollectionId)).ToList();

		RootStack.Add(new Label { Text = "Dashboard", FontSize = 20, FontAttributes = FontAttributes.Bold });

		// 2x2-Raster aus vier Statistik-Kacheln
		var stats = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
		stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		stats.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		stats.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

		stats.Add(StatCard("🗂️", mine.Count.ToString(), "Sammlungen"), 0, 0);
		stats.Add(StatCard("🔬", myObjects.Count.ToString(), "Exponate"), 1, 0);
		stats.Add(StatCard("📤", data.GetBorrowedCount(auth.CurrentUser!.Id).ToString(), "Ausgeliehen"), 0, 1);
		stats.Add(StatCard("⭐", data.GetFavorites(auth.CurrentUser.Id).Count.ToString(), "Favoriten"), 1, 1);
		RootStack.Add(stats);

		var popular = data.GetPopularCollections();
		RootStack.Add(new Label { Text = "Populäre öffentliche Sammlungen", FontSize = 18, FontAttributes = FontAttributes.Bold });
		if (popular.Count == 0)
			RootStack.Add(Muted("Noch keine öffentlichen Sammlungen vorhanden."));
		foreach (var collection in popular)
			RootStack.Add(Row(collection.Name, $"{collection.OwnerName} · {data.GetObjectCountForCollection(collection.Id)} Exponate"));
	}
	*/

	//08.07.2026 Alexander Stojek (Feinschliff): Neu – Begrüßung, Kacheln (bleiben), Schnellzugriff, Vorschau „Meine Sammlungen“, populäre Sammlungen.
	void RenderHome()
	{
		var mine = data.GetMyCollections(auth.CurrentUser!.Id);
		var myObjects = data.GetObjects().Where(o => mine.Any(c => c.Id == o.CollectionId)).ToList();

		// Begrüßung
		RootStack.Add(new Label { Text = $"Hallo, {auth.CurrentUser.Username} 👋", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1B4332") });
		RootStack.Add(Muted("Willkommen in deiner zoologischen Sammlung."));

		// 2x2-Raster aus vier Statistik-Kacheln (bleiben wie gewünscht)
		var stats = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
		stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		stats.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		stats.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		stats.Add(StatCard("🗂️", mine.Count.ToString(), "Sammlungen"), 0, 0);
		stats.Add(StatCard("🔬", myObjects.Count.ToString(), "Exponate"), 1, 0);
		stats.Add(StatCard("📤", data.GetBorrowedCount(auth.CurrentUser.Id).ToString(), "Ausgeliehen"), 0, 1);
		stats.Add(StatCard("⭐", data.GetFavorites(auth.CurrentUser.Id).Count.ToString(), "Favoriten"), 1, 1);
		RootStack.Add(stats);

		//08.07.2026 Alexander Stojek (Feinschliff): Schnellzugriff auf Wunsch entfernt (auskommentiert). Alt:
		/*
		// Schnellzugriff
		RootStack.Add(new Label { Text = "Schnellzugriff", FontSize = 18, FontAttributes = FontAttributes.Bold });
		var quick = new Grid { ColumnSpacing = 12 };
		quick.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		quick.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		quick.Add(Button("+ Neue Sammlung", () => { showNewCollection = true; _ = Shell.Current.GoToAsync("//collections"); }), 0, 0);
		quick.Add(Button("Suchen", () => { _ = Shell.Current.GoToAsync("//search"); }), 1, 0);
		RootStack.Add(quick);
		*/

		// Vorschau: Meine Sammlungen (erste drei)
		RootStack.Add(new Label { Text = "Meine Sammlungen", FontSize = 18, FontAttributes = FontAttributes.Bold });
		if (mine.Count == 0)
			RootStack.Add(Muted("Noch keine Sammlungen. Lege im Tab „Sammlungen“ eine an."));
		foreach (var c in mine.Take(3))
			RootStack.Add(Row(c.Name, $"{data.GetObjectCountForCollection(c.Id)} Exponate · {(c.IsPublic ? "öffentlich" : "privat")}", [
				//08.07.2026 Alexander Stojek (Feinschliff): beim Öffnen das Exponat-Formular eingeklappt starten.
				Button("Öffnen", () => { selectedCollectionId = c.Id; showNewObject = false; Show("collectionDetail"); })
			]));

		//08.07.2026 Alexander Stojek (Feinschliff): „Populäre öffentliche Sammlungen“ auf Wunsch entfernt (auskommentiert). Alt:
		/*
		// Populäre öffentliche Sammlungen
		var popular = data.GetPopularCollections();
		RootStack.Add(new Label { Text = "Populäre öffentliche Sammlungen", FontSize = 18, FontAttributes = FontAttributes.Bold });
		if (popular.Count == 0)
			RootStack.Add(Muted("Noch keine öffentlichen Sammlungen vorhanden."));
		foreach (var collection in popular)
			RootStack.Add(Row(collection.Name, $"{collection.OwnerName} · {data.GetObjectCountForCollection(collection.Id)} Exponate"));
		*/
	}

	void RenderAuth()
	{
		if (auth.IsLoggedIn)
		{
			RootStack.Add(Panel("Angemeldet", $"{auth.CurrentUser!.Username}\n{auth.CurrentUser.Email}\nRolle: {auth.CurrentUser.Role}", Button("Logout", () =>
			{
				auth.Logout();
				Show("auth");
			})));
			//08.07.2026 Alexander Stojek (Struktur-Umbau): zurück ins Menü „Mehr“.
			RootStack.Add(Button("← Zurück zu Mehr", () => Show("more")));
			return;
		}

		var email = Entry("E-Mail", "demo@zoolog.app");
		var password = Entry("Passwort", "demo123");
		password.IsPassword = true;
		RootStack.Add(Form("Login", [email, password], Button("Einloggen", () =>
		{
			var result = auth.Login(email.Text, password.Text);
			//08.07.2026 Alexander Stojek: Nach Login denselben Tab neu aufbauen -> zeigt "Angemeldet".
			if (!result.ok) Alert(result.error);
			else Show("auth");
		})));

		var name = Entry("Benutzername");
		var regEmail = Entry("E-Mail");
		var regPassword = Entry("Passwort");
		regPassword.IsPassword = true;
		RootStack.Add(Form("Registrieren", [name, regEmail, regPassword], Button("Konto erstellen", () =>
		{
			var result = auth.Register(name.Text, regEmail.Text, regPassword.Text);
			Alert(result.ok ? "Konto erstellt" : result.error);
			if (result.ok) Show("auth");
		})));
	}

	//08.07.2026 Alexander Stojek (Feinschliff): Sammlungen-Seite aufgeräumt. Alt (Formular immer offen, alle Sammlungen):
	/*
	void RenderCollections()
	{
		var name = Entry("Name der Sammlung");
		var desc = Editor("Beschreibung");
		var isPublic = new Switch { IsToggled = true };
		RootStack.Add(Form("Neue Sammlung", [name, desc, LabeledSwitch("Öffentlich", isPublic)], Button("Speichern", () =>
		{
			if (string.IsNullOrWhiteSpace(name.Text)) return;
			data.CreateCollection(name.Text, desc.Text ?? "", isPublic.IsToggled, auth.CurrentUser!.Id, auth.CurrentUser.Username);
			Show("collections");
		})));

		foreach (var c in data.GetCollections())
		{
			var favText = data.IsFavorite(auth.CurrentUser!.Id, c.Id) ? "Favorit entfernen" : "Favorit";
			RootStack.Add(Row(c.Name, $"{c.Description}\n{data.GetObjectCountForCollection(c.Id)} Exponate · {(c.IsPublic ? "öffentlich" : "privat")}", [
				Button("Öffnen", () => { selectedCollectionId = c.Id; Show("collectionDetail"); }),
				Button(favText, () => { data.ToggleFavorite(auth.CurrentUser!.Id, c.Id); Show("collections"); }),
				Button("Löschen", () => { data.DeleteCollection(c.Id); Show("collections"); }, true)
			]));
		}
	}
	*/

	//08.07.2026 Alexander Stojek (Feinschliff): Neu – Formular erst nach Klick auf „+ Neue Sammlung anlegen“; darunter Überschrift „Meine Sammlungen“ + eigene Sammlungen.
	void RenderCollections()
	{
		RootStack.Add(Button(showNewCollection ? "Abbrechen" : "+ Neue Sammlung anlegen", () =>
		{
			showNewCollection = !showNewCollection;
			Show("collections");
		}));

		if (showNewCollection)
		{
			var name = Entry("Name der Sammlung");
			var desc = Editor("Beschreibung");
			var isPublic = new Switch { IsToggled = true };
			RootStack.Add(Form("Neue Sammlung", [name, desc, LabeledSwitch("Öffentlich", isPublic)], Button("Speichern", () =>
			{
				if (string.IsNullOrWhiteSpace(name.Text)) return;
				data.CreateCollection(name.Text, desc.Text ?? "", isPublic.IsToggled, auth.CurrentUser!.Id, auth.CurrentUser.Username);
				showNewCollection = false;
				Show("collections");
			})));
		}

		RootStack.Add(new Label { Text = "Meine Sammlungen", FontSize = 20, FontAttributes = FontAttributes.Bold });

		var mine = data.GetMyCollections(auth.CurrentUser!.Id);
		if (mine.Count == 0)
			RootStack.Add(Muted("Du hast noch keine Sammlungen. Tippe oben auf „+ Neue Sammlung anlegen“."));

		foreach (var c in mine)
		{
			var favText = data.IsFavorite(auth.CurrentUser!.Id, c.Id) ? "Favorit entfernen" : "Favorit";
			RootStack.Add(Row(c.Name, $"{c.Description}\n{data.GetObjectCountForCollection(c.Id)} Exponate · {(c.IsPublic ? "öffentlich" : "privat")}", [
				Button("Öffnen", () => { selectedCollectionId = c.Id; showNewObject = false; Show("collectionDetail"); }),
				Button(favText, () => { data.ToggleFavorite(auth.CurrentUser!.Id, c.Id); Show("collections"); }),
				Button("Löschen", () => { data.DeleteCollection(c.Id); Show("collections"); }, true)
			]));
		}
	}

	//08.07.2026 Alexander Stojek (Struktur-Umbau): Detailansicht einer Sammlung. Alt (großer Zurück-Button, Panel, Formular immer offen):
	/*
	void RenderCollectionDetail()
	{
		var collection = data.GetCollections().FirstOrDefault(c => c.Id == selectedCollectionId);
		if (collection is null) { Show("collections"); return; }

		RootStack.Add(Button("← Zurück zu Sammlungen", () => Show("collections")));
		RootStack.Add(Panel(collection.Name, $"{collection.Description}\n{(collection.IsPublic ? "öffentlich" : "privat")} · von {collection.OwnerName}"));

		var isOwner = auth.CurrentUser is not null && collection.OwnerId == auth.CurrentUser.Id;
		if (isOwner)
		{
			var name = Entry("Name des Exponats");
			var locations = data.GetLocations();
			var locPicker = Picker("Fundort", ["Kein Fundort", .. locations.Select(l => l.Name)]);
			var art = Entry("Art");
			var gattung = Entry("Gattung");
			var familie = Entry("Familie");
			var notes = Editor("Notizen");
			RootStack.Add(Form("Neues Exponat", [name, locPicker, art, gattung, familie, notes], Button("Speichern", () =>
			{
				if (string.IsNullOrWhiteSpace(name.Text)) return;
				var locationId = locPicker.SelectedIndex > 0 ? locations[locPicker.SelectedIndex - 1].Id : "";
				data.CreateObject(new ZoologObject
				{
					Name = name.Text.Trim(),
					CollectionId = collection.Id,
					LocationId = locationId,
					Art = art.Text?.Trim() ?? "",
					Gattung = gattung.Text?.Trim() ?? "",
					Familie = familie.Text?.Trim() ?? "",
					Notes = notes.Text?.Trim() ?? "",
					CreatedBy = auth.CurrentUser!.Id
				});
				Show("collectionDetail");
			})));
		}

		var objs = data.GetObjectsInCollection(collection.Id);
		RootStack.Add(new Label { Text = $"Exponate ({objs.Count})", FontSize = 18, FontAttributes = FontAttributes.Bold });
		if (objs.Count == 0)
			RootStack.Add(Muted("Noch keine Exponate in dieser Sammlung."));
		foreach (var obj in objs)
		{
			var actions = new List<View> { Button("Öffnen", () => { selectedObjectId = obj.Id; Show("objectDetail"); }) };
			if (isOwner)
				actions.Add(Button("Löschen", () => { data.DeleteObject(obj.Id); Show("collectionDetail"); }, true));
			RootStack.Add(Row(obj.Name, $"{obj.Status} · {obj.Gattung} {obj.Art}\n{obj.Notes}", actions));
		}
	}
	*/

	//08.07.2026 Alexander Stojek (Feinschliff): Neu – kleiner Zurück-Link oben links, „+ Neues Exponat“ erst per Button, dann Überschrift „Exponate“, darunter die Liste.
	void RenderCollectionDetail()
	{
		var collection = data.GetCollections().FirstOrDefault(c => c.Id == selectedCollectionId);
		if (collection is null) { Show("collections"); return; }

		// Kleiner, unauffälliger Zurück-Link oben links
		RootStack.Add(BackLink("← zurück", () => Show("collections")));
		RootStack.Add(new Label { Text = collection.Name, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1B4332") });

		var isOwner = auth.CurrentUser is not null && collection.OwnerId == auth.CurrentUser.Id;
		if (isOwner)
		{
			RootStack.Add(Button(showNewObject ? "Abbrechen" : "+ Neues Exponat", () =>
			{
				showNewObject = !showNewObject;
				Show("collectionDetail");
			}));

			if (showNewObject)
			{
				var name = Entry("Name des Exponats");
				var locations = data.GetLocations();
				var locPicker = Picker("Fundort", ["Kein Fundort", .. locations.Select(l => l.Name)]);
				var art = Entry("Art");
				var gattung = Entry("Gattung");
				var familie = Entry("Familie");
				var notes = Editor("Notizen");
				RootStack.Add(Form("Neues Exponat", [name, locPicker, art, gattung, familie, notes], Button("Speichern", () =>
				{
					if (string.IsNullOrWhiteSpace(name.Text)) return;
					var locationId = locPicker.SelectedIndex > 0 ? locations[locPicker.SelectedIndex - 1].Id : "";
					data.CreateObject(new ZoologObject
					{
						Name = name.Text.Trim(),
						CollectionId = collection.Id,
						LocationId = locationId,
						Art = art.Text?.Trim() ?? "",
						Gattung = gattung.Text?.Trim() ?? "",
						Familie = familie.Text?.Trim() ?? "",
						Notes = notes.Text?.Trim() ?? "",
						CreatedBy = auth.CurrentUser!.Id
					});
					showNewObject = false;
					Show("collectionDetail");
				})));
			}
		}

		RootStack.Add(new Label { Text = "Exponate", FontSize = 18, FontAttributes = FontAttributes.Bold });

		var objs = data.GetObjectsInCollection(collection.Id);
		if (objs.Count == 0)
			RootStack.Add(Muted("Noch keine Exponate in dieser Sammlung."));
		foreach (var obj in objs)
		{
			var actions = new List<View> { Button("Öffnen", () => { selectedObjectId = obj.Id; Show("objectDetail"); }) };
			if (isOwner)
				actions.Add(Button("Löschen", () => { data.DeleteObject(obj.Id); Show("collectionDetail"); }, true));
			RootStack.Add(Row(obj.Name, $"{obj.Status} · {obj.Gattung} {obj.Art}\n{obj.Notes}", actions));
		}
	}

	//08.07.2026 Alexander Stojek (Struktur-Umbau): Globale Suche über Exponate, Sammlungen und Fundorte (Live-Filter während des Tippens).
	void RenderSearch()
	{
		var box = new SearchBar { Placeholder = "Exponate, Sammlungen, Fundorte suchen…", BackgroundColor = Colors.White };
		RootStack.Add(box);

		var results = new VerticalStackLayout { Spacing = 12 };
		RootStack.Add(results);

		box.TextChanged += (_, e) => RenderSearchResults(results, e.NewTextValue);
		RenderSearchResults(results, "");
	}

	void RenderSearchResults(VerticalStackLayout container, string? query)
	{
		container.Clear();
		var q = (query ?? "").Trim().ToLower();
		if (q.Length == 0) { container.Add(Muted("Tippe oben, um zu suchen.")); return; }

		var cols = data.GetCollections().Where(c => (c.Name ?? "").ToLower().Contains(q)).ToList();
		var objs = data.GetObjects().Where(o => (o.Name ?? "").ToLower().Contains(q) || (o.Gattung ?? "").ToLower().Contains(q) || (o.Art ?? "").ToLower().Contains(q)).ToList();
		var locs = data.GetLocations().Where(l => (l.Name ?? "").ToLower().Contains(q)).ToList();

		if (cols.Count + objs.Count + locs.Count == 0) { container.Add(Muted("Keine Treffer.")); return; }

		if (cols.Count > 0) container.Add(new Label { Text = "Sammlungen", FontAttributes = FontAttributes.Bold });
		foreach (var c in cols) container.Add(Row(c.Name, c.Description ?? ""));

		if (objs.Count > 0) container.Add(new Label { Text = "Exponate", FontAttributes = FontAttributes.Bold });
		foreach (var o in objs) container.Add(Row(o.Name, $"{o.Gattung} {o.Art} · {o.Status}"));

		if (locs.Count > 0) container.Add(new Label { Text = "Fundorte", FontAttributes = FontAttributes.Bold });
		foreach (var l in locs) container.Add(Row(l.Name, $"{l.Building} · {l.Room} · {l.Shelf}"));
	}

	//08.07.2026 Alexander Stojek (Struktur-Umbau): Sammel-Tab „Mehr“ – Konto und Leihgaben (Einstellungen folgen später).
	void RenderMore()
	{
		RootStack.Add(new Label { Text = "Mehr", FontSize = 20, FontAttributes = FontAttributes.Bold });
		RootStack.Add(Panel("Konto", auth.IsLoggedIn ? $"Angemeldet als {auth.CurrentUser!.Username}" : "Nicht angemeldet", Button("Öffnen", () => Show("auth"))));
		RootStack.Add(Panel("Leihgaben", "Ausleihen anlegen und zurücknehmen.", Button("Öffnen", () => Show("loans"))));
		//08.07.2026 Alexander Stojek (Feature B): Einstellungen (Benachrichtigungs-Schalter).
		RootStack.Add(Panel("Einstellungen", "Benachrichtigungen an/aus.", Button("Öffnen", () => Show("settings"))));
	}

	//08.07.2026 Alexander Stojek (Feature B): Einstellungen – Schalter für Benachrichtigungen (wird in Preferences gespeichert).
	// Die eigentlichen Erinnerungen bei ablaufenden Leihfristen kommen in einem späteren Schritt; dieser Schalter steuert später, ob sie geplant werden.
	void RenderSettings()
	{
		RootStack.Add(Button("← Zurück zu Mehr", () => Show("more")));

		var toggle = new Switch { IsToggled = Preferences.Get("notifications_enabled", true) };
		toggle.Toggled += (_, e) => Preferences.Set("notifications_enabled", e.Value);

		var stack = Card();
		stack.Add(new Label { Text = "Einstellungen", FontSize = 18, FontAttributes = FontAttributes.Bold });
		stack.Add(new Label { Text = "Benachrichtigungen bei bald ablaufenden Leihfristen.", TextColor = Color.FromArgb("#4B5563") });
		stack.Add(LabeledSwitch("Benachrichtigungen aktiviert", toggle));
		RootStack.Add(Wrap(stack));
	}

	//08.07.2026 Alexander Stojek (Feature B): Detail- und Bearbeiten-Ansicht eines Exponats (nur der Eigentümer kann bearbeiten).
	void RenderObjectDetail()
	{
		var obj = data.GetObjects().FirstOrDefault(o => o.Id == selectedObjectId);
		if (obj is null) { Show("collectionDetail"); return; }

		RootStack.Add(Button("← Zurück", () => Show("collectionDetail")));
		RootStack.Add(Panel(obj.Name, $"Status: {obj.Status}\nGattung/Art: {obj.Gattung} {obj.Art}\nFamilie: {obj.Familie}\n\n{obj.Notes}"));

		var collection = data.GetCollections().FirstOrDefault(c => c.Id == obj.CollectionId);
		var isOwner = auth.CurrentUser is not null && collection is not null && collection.OwnerId == auth.CurrentUser.Id;
		if (!isOwner)
			return;

		var name = Entry("Name", obj.Name);
		var art = Entry("Art", obj.Art);
		var gattung = Entry("Gattung", obj.Gattung);
		var familie = Entry("Familie", obj.Familie);
		var statusOptions = new List<string> { "verfügbar", "ausgeliehen", "verloren", "zerstört" };
		var statusPicker = Picker("Status", statusOptions);
		statusPicker.SelectedIndex = Math.Max(0, statusOptions.IndexOf(obj.Status));
		var notes = Editor("Notizen");
		notes.Text = obj.Notes;

		RootStack.Add(Form("Bearbeiten", [name, art, gattung, familie, statusPicker, notes], Button("Speichern", () =>
		{
			if (string.IsNullOrWhiteSpace(name.Text)) return;
			obj.Name = name.Text.Trim();
			obj.Art = art.Text?.Trim() ?? "";
			obj.Gattung = gattung.Text?.Trim() ?? "";
			obj.Familie = familie.Text?.Trim() ?? "";
			obj.Status = statusPicker.SelectedIndex >= 0 ? statusOptions[statusPicker.SelectedIndex] : obj.Status;
			obj.Notes = notes.Text?.Trim() ?? "";
			data.UpdateObject(obj);
			Show("objectDetail");
		})));
	}

	void RenderObjects()
	{
		var collections = data.GetMyCollections(auth.CurrentUser!.Id);
		if (collections.Count == 0)
		{
			//08.07.2026 Alexander Stojek: Verweist auf den Sammlungen-Tab statt auf einen Button.
			RootStack.Add(Panel("Keine Sammlung", "Lege zuerst über den Tab „Sammlungen“ eine Sammlung an."));
			return;
		}

		var name = Entry("Name des Exponats");
		var colPicker = Picker("Sammlung", collections.Select(c => c.Name).ToList());
		var locations = data.GetLocations();
		var locPicker = Picker("Standort", ["Kein Standort", .. locations.Select(l => l.Name)]);
		var art = Entry("Art");
		var gattung = Entry("Gattung");
		var familie = Entry("Familie");
		var notes = Editor("Notizen");
		RootStack.Add(Form("Neues Exponat", [name, colPicker, locPicker, art, gattung, familie, notes], Button("Speichern", () =>
		{
			if (string.IsNullOrWhiteSpace(name.Text)) return;
			var collection = collections[Math.Max(0, colPicker.SelectedIndex)];
			var locationId = locPicker.SelectedIndex > 0 ? locations[locPicker.SelectedIndex - 1].Id : "";
			data.CreateObject(new ZoologObject
			{
				Name = name.Text.Trim(),
				CollectionId = collection.Id,
				LocationId = locationId,
				Art = art.Text?.Trim() ?? "",
				Gattung = gattung.Text?.Trim() ?? "",
				Familie = familie.Text?.Trim() ?? "",
				Notes = notes.Text?.Trim() ?? "",
				CreatedBy = auth.CurrentUser!.Id
			});
			Show("objects");
		})));

		var mine = collections.Select(c => c.Id).ToHashSet();
		foreach (var obj in data.GetObjects().Where(o => mine.Contains(o.CollectionId)))
			RootStack.Add(Row(obj.Name, $"{obj.Status} · {obj.Gattung} {obj.Art}\n{obj.Notes}", [
				Button("Löschen", () => { data.DeleteObject(obj.Id); Show("objects"); }, true)
			]));
	}

	void RenderLoans()
	{
		var collectionIds = data.GetMyCollections(auth.CurrentUser!.Id).Select(c => c.Id).ToHashSet();
		var available = data.GetObjects().Where(o => collectionIds.Contains(o.CollectionId) && o.Status == "verfügbar").ToList();
		if (available.Count > 0)
		{
			var objectPicker = Picker("Exponat", available.Select(o => o.Name).ToList());
			var borrower = Entry("Ausgeliehen an");
			var notes = Editor("Notizen");
			RootStack.Add(Form("Neue Leihgabe", [objectPicker, borrower, notes], Button("Ausleihen", () =>
			{
				if (string.IsNullOrWhiteSpace(borrower.Text)) return;
				data.CreateLoan(available[Math.Max(0, objectPicker.SelectedIndex)].Id, borrower.Text, DateTime.Today.AddDays(14), notes.Text ?? "", auth.CurrentUser!.Id);
				Show("loans");
			})));
		}

		var objectIds = data.GetObjects().Where(o => collectionIds.Contains(o.CollectionId)).Select(o => o.Id).ToHashSet();
		foreach (var loan in data.GetOpenLoans().Where(l => objectIds.Contains(l.ObjectId)))
			RootStack.Add(Row(ObjectName(loan.ObjectId), $"{loan.BorrowerName} · bis {loan.DueAt?.ToLocalTime():dd.MM.yyyy}\n{loan.Notes}", [
				Button("Zurück", () => { data.ReturnLoan(loan.Id); Show("loans"); })
			]));
	}

	void RenderLocations()
	{
		var name = Entry("Standortname");
		var building = Entry("Gebäude");
		var room = Entry("Raum");
		var shelf = Entry("Regal / Fach");
		var notes = Editor("Notizen");
		RootStack.Add(Form("Neuer Standort", [name, building, room, shelf, notes], Button("Speichern", () =>
		{
			if (string.IsNullOrWhiteSpace(name.Text)) return;
			data.CreateLocation(name.Text, building.Text ?? "", room.Text ?? "", shelf.Text ?? "", notes.Text ?? "");
			Show("locations");
		})));

		foreach (var location in data.GetLocations())
			RootStack.Add(Row(location.Name, $"{location.Building} · {location.Room} · {location.Shelf}\n{data.GetObjects().Count(o => o.LocationId == location.Id)} Exponate", [
				Button("Löschen", () => { data.DeleteLocation(location.Id); Show("locations"); }, true)
			]));
	}

	void Show(string target)
	{
		view = target;
		Render();
		RootScroll.ScrollToAsync(0, 0, false);
	}

	Button Button(string text, Action action, bool danger = false)
	{
		var button = new Button
		{
			Text = text,
			CornerRadius = 8,
			BackgroundColor = danger ? Color.FromArgb("#C2413D") : Color.FromArgb("#2D6A4F"),
			TextColor = Colors.White
		};
		button.Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.20f, Radius = 6, Offset = new Point(0, 2) };
		button.Clicked += (_, _) => action();
		return button;
	}

	Entry Entry(string placeholder, string text = "") => new()
	{
		Placeholder = placeholder,
		//08.07.2026 Alexander Stojek (Feinschliff): Beispieltext gut lesbar in mittlerem Grau (nicht so kräftig wie normale Schrift, aber nicht fast unsichtbar).
		PlaceholderColor = Color.FromArgb("#6B7280"),
		Text = text,
		//08.07.2026 Alexander Stojek (Feinschliff): transparent, damit die neue Feld-Box (FieldWrap) sichtbar ist.
		BackgroundColor = Colors.Transparent
	};

	Editor Editor(string placeholder) => new()
	{
		Placeholder = placeholder,
		//08.07.2026 Alexander Stojek (Feinschliff): Beispieltext gut lesbar in mittlerem Grau.
		PlaceholderColor = Color.FromArgb("#6B7280"),
		AutoSize = EditorAutoSizeOption.TextChanges,
		MinimumHeightRequest = 90,
		//08.07.2026 Alexander Stojek (Feinschliff): transparent, damit die neue Feld-Box (FieldWrap) sichtbar ist.
		BackgroundColor = Colors.Transparent
	};

	Picker Picker(string title, List<string> items)
	{
		//08.07.2026 Alexander Stojek (Feinschliff): transparent, damit die neue Feld-Box (FieldWrap) sichtbar ist; Titel in mittlerem Grau.
		var picker = new Picker { Title = title, TitleColor = Color.FromArgb("#6B7280"), BackgroundColor = Colors.Transparent };
		foreach (var item in items) picker.Items.Add(item);
		if (items.Count > 0) picker.SelectedIndex = 0;
		return picker;
	}

	View Form(string title, IEnumerable<View> fields, Button submit)
	{
		var stack = Card();
		stack.Add(new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold });
		//08.07.2026 Alexander Stojek (Feinschliff): Jedes Feld hübsch verpacken. Alt: foreach (var field in fields) stack.Add(field);
		foreach (var field in fields) stack.Add(FieldWrap(field));
		stack.Add(submit);
		return Wrap(stack);
	}

	//08.07.2026 Alexander Stojek (Feinschliff): Alt – Feld mit Überschrift statt Platzhalter (Beispieltext war dadurch weg):
	/*
	View FieldWrap(View field)
	{
		string? label = null;
		if (field is Entry e) { label = e.Placeholder; e.Placeholder = ""; }
		else if (field is Editor ed) { label = ed.Placeholder; ed.Placeholder = ""; }
		else if (field is Picker p) { label = p.Title; }

		if (label is null)
			return field;

		var box = new VerticalStackLayout { Spacing = 4 };
		box.Add(new Label { Text = label, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#374151") });
		box.Add(new Border
		{
			Content = field,
			BackgroundColor = Color.FromArgb("#F3F4F6"),
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			Padding = new Thickness(12, 4)
		});
		return box;
	}
	*/

	//08.07.2026 Alexander Stojek (Feinschliff): Neu – bekannte Eingabefelder nur in eine gefüllte, abgerundete Box packen. Der Beispieltext (Platzhalter) bleibt im Feld sichtbar.
	View FieldWrap(View field)
	{
		// Alles außer bekannten Eingabefeldern (z. B. Schalter) unverändert lassen.
		//08.07.2026 Alexander Stojek (Feinschliff): voll qualifizierte Typnamen, weil unsere Helfer Entry()/Editor()/Picker() genauso heißen wie die Typen.
		if (field is not (Microsoft.Maui.Controls.Entry or Microsoft.Maui.Controls.Editor or Microsoft.Maui.Controls.Picker))
			return field;

		return new Border
		{
			Content = field,
			BackgroundColor = Color.FromArgb("#F3F4F6"),
			StrokeThickness = 0,
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			Padding = new Thickness(12, 4)
		};
	}

	View Panel(string title, string body, View? action = null)
	{
		var stack = Card();
		stack.Add(new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold });
		stack.Add(new Label { Text = body, TextColor = Color.FromArgb("#4B5563") });
		if (action != null) stack.Add(action);
		return Wrap(stack);
	}

	View Row(string title, string detail, IEnumerable<View>? actions = null)
	{
		var stack = Card();
		stack.Add(new Label { Text = title, FontAttributes = FontAttributes.Bold });
		stack.Add(new Label { Text = detail, TextColor = Color.FromArgb("#6B7280") });
		if (actions != null)
		{
			var row = new HorizontalStackLayout { Spacing = 8 };
			foreach (var action in actions) row.Add(action);
			stack.Add(row);
		}
		return Wrap(stack);
	}

	VerticalStackLayout Card() => new()
	{
		Spacing = 10,
		Padding = 14,
		BackgroundColor = Colors.Transparent
	};

	Border Wrap(View inner) => new()
	{
		Content = inner,
		BackgroundColor = Colors.White,
		StrokeThickness = 0,
		StrokeShape = new RoundRectangle { CornerRadius = 14 },
		Shadow = new Shadow
		{
			Brush = Brush.Black,
			Opacity = 0.15f,
			Radius = 12,
			Offset = new Point(0, 4)
		}
	};

	View StatCard(string emoji, string value, string label)
	{
		var stack = new VerticalStackLayout { Spacing = 4, Padding = 14, HorizontalOptions = LayoutOptions.Center };
		stack.Add(new Label { Text = emoji, FontSize = 30, HorizontalOptions = LayoutOptions.Center });
		stack.Add(new Label { Text = value, FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1B4332"), HorizontalOptions = LayoutOptions.Center });
		stack.Add(new Label { Text = label, FontSize = 13, TextColor = Color.FromArgb("#6B7280"), HorizontalOptions = LayoutOptions.Center });
		return Wrap(stack);
	}

	View LabeledSwitch(string text, Switch control) => new HorizontalStackLayout
	{
		Spacing = 10,
		Children = { control, new Label { Text = text, VerticalOptions = LayoutOptions.Center } }
	};

	//08.07.2026 Alexander Stojek (Feinschliff): Kleiner, unauffälliger Zurück-Link (statt großer Button), linksbündig oben.
	View BackLink(string text, Action action)
	{
		var link = new Label
		{
			Text = text,
			FontSize = 14,
			TextColor = Color.FromArgb("#6B7280"),
			HorizontalOptions = LayoutOptions.Start
		};
		var tap = new TapGestureRecognizer();
		tap.Tapped += (_, _) => action();
		link.GestureRecognizers.Add(tap);
		return link;
	}

	Label Muted(string text) => new() { Text = text, TextColor = Color.FromArgb("#6B7280") };
	string ObjectName(string id) => data.GetObjects().FirstOrDefault(o => o.Id == id)?.Name ?? "Unbekanntes Exponat";
	void Alert(string message) => _ = DisplayAlertAsync("Zoolog", message, "OK");
}
