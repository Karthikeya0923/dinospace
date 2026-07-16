using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace dinospace
{
    // Composition root shared by every platform: registers the app class,
    // the camera toolkit, and the two Baloo fonts. Kept deliberately tiny —
    // services in this app are plain static classes, not DI registrations,
    // so a four-line builder is all the startup wiring there is.
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                // live camera preview behind the Scan Sky overlay
                .UseMauiCommunityToolkitCamera()
                .ConfigureFonts(fonts =>
                {
                    // One rounded family everywhere, straight from the design
                    // sheet: Baloo 2 Bold for headlines, Baloo 2 Medium for
                    // body text — no plain "Arial-looking" sans anywhere.
                    fonts.AddFont("Baloo2-Medium.ttf", "Body");
                    fonts.AddFont("Baloo2-Bold.ttf", "Baloo");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
