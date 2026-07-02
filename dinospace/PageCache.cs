using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace dinospace
{
    // Opening a page used to mean building it from scratch on the spot -
    // inflate the XAML, lay everything out, decode images - all before the
    // slide-in animation could even start. That's where the "tap... wait...
    // page" feeling came from.
    //
    // This cache fixes it two ways:
    //   1. The heavy pages (DinoPedia, SpacePedia, Ask AI) are built ONCE
    //      and reused, so every open after the first is instant.
    //   2. Warmup() quietly pre-builds them right after the app launches,
    //      so even the FIRST open is instant.
    //
    // Bonus: because AskAiPage is reused, the NovaSaur chat stays exactly
    // where you left it when you pop back in.
    public static class PageCache
    {
        private static DinoPediaPage _dinoPedia;
        private static SpacePediaPage _spacePedia;
        private static AskAiPage _askAi;

        public static DinoPediaPage DinoPedia => _dinoPedia ??= new DinoPediaPage();
        public static SpacePediaPage SpacePedia => _spacePedia ??= new SpacePediaPage();
        public static AskAiPage AskAi => _askAi ??= new AskAiPage();

        // Pushes a cached page, guarding against double-taps and against
        // pushing a page that's already on the navigation stack.
        public static async System.Threading.Tasks.Task PushAsync(Page page)
        {
            if (page == null) return;
            if (page.Parent != null) return; // already showing / mid-transition
            try
            {
                await Shell.Current.Navigation.PushAsync(page);
            }
            catch { }
        }

        // Builds the heavy pages in the background shortly after launch,
        // one per UI-thread idle slot so startup itself stays smooth.
        public static void Warmup(IDispatcher dispatcher)
        {
            if (dispatcher == null) return;
            dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(900), () =>
            {
                try { _ = DinoPedia; } catch { }
                dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () =>
                {
                    try { _ = SpacePedia; } catch { }
                    dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () =>
                    {
                        try { _ = AskAi; } catch { }
                    });
                });
            });
        }
    }
}
