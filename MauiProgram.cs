using Microsoft.Extensions.Logging;
using ProTrack.Maui.Pages;
using ProTrack.Maui.Services;

namespace ProTrack.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});


        builder.Services.AddSingleton<AuthService>(sp =>
        {
            return new AuthService(
                Preferences.Default.Get(
                    "ServerUrl",
                    "http://localhost:5081"));
        });

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<HomePage>();


#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
