using ZoologAPP.Models;
using ZoologAPP.Services;
//Alexander Stojek 08.07.2026: Shapes hinzugefügt
using Microsoft.Maui.Controls.Shapes;


namespace ZoologAPP;

public partial class MainPage : ContentPage
{
	readonly AuthService auth = new();
	readonly DataService data = new();
	string view = "home";

	public MainPage()
	{
		InitializeComponent();
		Render();
	}

	void Render()
	{
		RootStack.Clear();
		RootStack.Add(Header());
		RootStack.Add(Nav());

		if (view != "auth" && !auth.IsLoggedIn)
		{
			RootStack.Add(Panel("Anmeldung nötig", "Bitte melde dich an, um Zoolog zu nutzen.", Button("Zur Anmeldung", () => Show("auth"))));
			return;
		}

		switch (view)
		{
			case "auth": RenderAuth(); break;
			case "collections": RenderCollections(); break;
			case "objects": RenderObjects(); break;
			case "loans": RenderLoans(); break;
			case "locations": RenderLocations(); break;
			default: RenderHome(); break;
		}
	}

	View Header()
	{
		var title = new Label { Text = "Zoolog", FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1B4332") };
		var sub = new Label { Text = auth.CurrentUser?.Username ?? "mobile sammlung", TextColor = Color.FromArgb("#6B7280") };
		return new VerticalStackLayout { Spacing = 2, Children = { title, sub } };
	}

	View Nav()
	{
		var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
		for (var i = 0; i < 3; i++)
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		for (var i = 0; i < 2; i++)
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

		AddNav(grid, "Home", "home", 0, 0);
		AddNav(grid, "Konto", "auth", 1, 0);
		AddNav(grid, "Sammlungen", "collections", 2, 0);
		AddNav(grid, "Exponate", "objects", 0, 1);
		AddNav(grid, "Leihgaben", "loans", 1, 1);
		AddNav(grid, "Standorte", "locations", 2, 1);
		return grid;
	}

	void AddNav(Grid grid, string text, string target, int col, int row)
	{
		var button = Button(text, () => Show(target));
		button.BackgroundColor = view == target ? Color.FromArgb("#1B4332") : Color.FromArgb("#EAF1ED");
		button.TextColor = view == target ? Colors.White : Color.FromArgb("#1B4332");
		grid.Add(button, col, row);
	}
	
	//Alexander Stojek 08.07.2026: Ersetzen der bisherigen RenderHome Methode durch eine neue Version
	//Alt:
	/* void RenderHome()
	{
		var mine = auth.CurrentUser is null ? [] : data.GetMyCollections(auth.CurrentUser.Id);
		var myObjects = data.GetObjects().Where(o => mine.Any(c => c.Id == o.CollectionId)).ToList();

		RootStack.Add(Panel("Dashboard",
			$"Sammlungen: {mine.Count}\nExponate: {myObjects.Count}\nAusgeliehen: {data.GetBorrowedCount(auth.CurrentUser!.Id)}\nFavoriten: {data.GetFavorites(auth.CurrentUser.Id).Count}"));

		var popular = data.GetPopularCollections();
		RootStack.Add(new Label { Text = "Populäre öffentliche Sammlungen", FontSize = 18, FontAttributes = FontAttributes.Bold });
		if (popular.Count == 0)
			RootStack.Add(Muted("Noch keine öffentlichen Sammlungen vorhanden."));
		foreach (var collection in popular)
			RootStack.Add(Row(collection.Name, $"{collection.OwnerName} · {data.GetObjectCountForCollection(collection.Id)} Exponate"));
	} */

	//Neu: 
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

		// Populäre öffentliche Sammlungen (wie gehabt, jetzt als Karten mit Schatten)
		var popular = data.GetPopularCollections();
		RootStack.Add(new Label { Text = "Populäre öffentliche Sammlungen", FontSize = 18, FontAttributes = FontAttributes.Bold });
		if (popular.Count == 0)
			RootStack.Add(Muted("Noch keine öffentlichen Sammlungen vorhanden."));
		foreach (var collection in popular)
			RootStack.Add(Row(collection.Name, $"{collection.OwnerName} · {data.GetObjectCountForCollection(collection.Id)} Exponate"));
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
			return;
		}

		var email = Entry("E-Mail", "demo@zoolog.app");
		var password = Entry("Passwort", "demo123");
		password.IsPassword = true;
		RootStack.Add(Form("Login", [email, password], Button("Einloggen", () =>
		{
			var result = auth.Login(email.Text, password.Text);
			if (!result.ok) Alert(result.error);
			else Show("home");
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
				Button(favText, () => { data.ToggleFavorite(auth.CurrentUser!.Id, c.Id); Show("collections"); }),
				Button("Löschen", () => { data.DeleteCollection(c.Id); Show("collections"); }, true)
			]));
		}
	}

	void RenderObjects()
	{
		var collections = data.GetMyCollections(auth.CurrentUser!.Id);
		if (collections.Count == 0)
		{
			RootStack.Add(Panel("Keine Sammlung", "Lege zuerst eine Sammlung an.", Button("Sammlungen öffnen", () => Show("collections"))));
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
		Text = text,
		BackgroundColor = Colors.White
	};

	Editor Editor(string placeholder) => new()
	{
		Placeholder = placeholder,
		AutoSize = EditorAutoSizeOption.TextChanges,
		MinimumHeightRequest = 90,
		BackgroundColor = Colors.White
	};

	Picker Picker(string title, List<string> items)
	{
		var picker = new Picker { Title = title, BackgroundColor = Colors.White };
		foreach (var item in items) picker.Items.Add(item);
		if (items.Count > 0) picker.SelectedIndex = 0;
		return picker;
	}

	View Form(string title, IEnumerable<View> fields, Button submit)
	{
		var stack = Card();
		stack.Add(new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold });
		foreach (var field in fields) stack.Add(field);
		stack.Add(submit);
		return Wrap(stack);
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
		//Alexander Stojek 08.07.2026: Color auf Transparent gesetzt
		BackgroundColor = Colors.Transparent
	};


	//Alexander Stojek 08.07.2026: Einfügen einer Wrap Methode die eine Karte anlegt die weiß und abgerundet ist und Schatten hat.
	Border Wrap(View inner) => new()
	{
		Content = inner,
		BackgroundColor = Colors.White,
		StrokeThickness = 0,                                  // keine sichtbare Rahmenlinie
		StrokeShape = new RoundRectangle { CornerRadius = 14 }, // runde Ecken
		Shadow = new Shadow
		{
			Brush = Brush.Black,        // Farbe des Schattens
			Opacity = 0.15f,            // 15% – dezent, nicht hart
			Radius = 12,                // Weichzeichnung (je höher, desto weicher)
			Offset = new Point(0, 4)    // 4 px nach unten versetzt → wirkt „schwebend"
		}
	};	

	//Alexander Stojek 08.07.2026: Einfügen einer Kachel, die ein Emoji, einen Wert und eine Beschriftung anzeigt. 
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

	Label Muted(string text) => new() { Text = text, TextColor = Color.FromArgb("#6B7280") };
	string ObjectName(string id) => data.GetObjects().FirstOrDefault(o => o.Id == id)?.Name ?? "Unbekanntes Exponat";
	void Alert(string message) => _ = DisplayAlertAsync("Zoolog", message, "OK");
}
