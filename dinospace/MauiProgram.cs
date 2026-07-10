using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace dinospace
{
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
