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
                .ConfigureFonts(fonts =>
                {
                    // Baloo2 -> friendly rounded headings; Nunito -> body text.
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
