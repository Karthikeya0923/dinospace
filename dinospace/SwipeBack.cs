using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace dinospace
{
    // Swipe-LEFT-to-go-back for pushed pages. PanGestureRecognizer coexists with ScrollView.
    public static class SwipeBack
    {
        public static void Attach(ContentPage page)
        {
            if (page?.Content is Layout root)
                Wire(root);
        }

        public static void Attach(Layout root) => Wire(root);

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
                        if (totalX < -55 && Math.Abs(totalX) > Math.Abs(totalY) * 1.2)
                        {
                            fired = true;
                            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
                            try
                            {
                                if (Shell.Current?.Navigation?.NavigationStack?.Count > 1)
                                    await Shell.Current.Navigation.PopAsync();
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