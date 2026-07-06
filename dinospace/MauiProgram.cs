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
                    // DM Serif Display -> editorial headlines; Nunito -> body;
                    // Material Icons -> tab bar and buttons.
                    fonts.AddFont("Nunito-Regular.ttf", "Nunito");
                    fonts.AddFont("DMSerifDisplay-Regular.ttf", "Serif");
                    fonts.AddFont("DMSerifDisplay-Italic.ttf", "SerifItalic");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "Icons");
                    fonts.AddFont("Baloo2-Bold.ttf", "Baloo");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
