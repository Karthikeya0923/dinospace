using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace dinospace
{
    // Edge swipe-back for pushed detail pages. A short right-swipe from the
    // left third of the screen pops the page, matching platform expectation.
    public static class SwipeBack
    {
        public static void Attach(ContentPage page)
        {
            if (page?.Content is Layout root)
                Wire(root);
        }

        private static void Wire(Layout root)
        {
            if (root == null) return;

            double totalX = 0, totalY = 0;
            bool fired = false;

            var pan = new PanGestureRecognizer();
            pan.PanUpdated += async (s, e) =>
            {
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        totalX = 0; totalY = 0; fired = false;
                        break;
                    case GestureStatus.Running:
                        totalX = e.TotalX;
                        totalY = e.TotalY;
                        break;
                    case GestureStatus.Completed:
                    case GestureStatus.Canceled:
                        if (fired) break;
                        // Rightward swipe, mostly horizontal.
                        if (totalX > 60 && System.Math.Abs(totalX) > System.Math.Abs(totalY) * 1.4)
                        {
                            fired = true;
                            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                            try
                            {
                                var nav = Shell.Current?.Navigation;
                                if (nav != null && nav.NavigationStack.Count > 1)
                                    await nav.PopAsync();
                            }
                            catch { }
                        }
                        break;
                }
            };

            root.GestureRecognizers.Add(pan);
        }
    }
}
