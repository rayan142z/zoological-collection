using Microsoft.Extensions.Logging;
using ZoologAPP.Services;
//08.07.2026 Alexander Stojek: Für echte lokale Benachrichtigungen (Einstellungen-Testbenachrichtigung).
using Plugin.LocalNotification;
//08.07.2026 Alexander Stojek: Für die Kartenansicht der Fundorte.
using Microsoft.Maui.Controls.Maps;

namespace ZoologAPP;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			//08.07.2026 Alexander Stojek: Aktiviert das Benachrichtigungs-Plugin für die App.
			.UseLocalNotification()
			//08.07.2026 Alexander Stojek: Aktiviert die Kartenansicht (Fundorte).
			.UseMauiMaps()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<DataService>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

#if ANDROID
		//08.07.2026 Alexander Stojek (Karte): Ohne diesen Fix "klaut" die scrollbare Seite der Karte die Zieh-Geste
		// (man kann die Karte dann nicht mit dem Finger verschieben). Der Fix sorgt dafür, dass die Karte, sobald man
		// sie berührt, das Verschieben für sich beansprucht statt es an die Seite weiterzugeben.
		Microsoft.Maui.Maps.Handlers.MapHandler.Mapper.AppendToMapping("FixMapDragInsideScrollView", (handler, view) =>
		{
			handler.PlatformView.Touch += (sender, e) =>
			{
				switch (e.Event?.Action)
				{
					case Android.Views.MotionEventActions.Down:
						handler.PlatformView.Parent?.RequestDisallowInterceptTouchEvent(true);
						break;
					case Android.Views.MotionEventActions.Up:
					case Android.Views.MotionEventActions.Cancel:
						handler.PlatformView.Parent?.RequestDisallowInterceptTouchEvent(false);
						break;
				}
			};
		});
#endif

		return builder.Build();
	}
}
