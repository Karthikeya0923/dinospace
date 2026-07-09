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
                    // Baloo -> rounded storybook headlines; Nunito -> body.
                    fonts.AddFont("Nunito-Regular.ttf", "Nunito");
                    fonts.AddFont("Baloo2-Bold.ttf", "Baloo");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
