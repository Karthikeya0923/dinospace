using Microsoft.Maui.Controls;

namespace dinospace
{
    // Adds left/right tab swiping to a page's root layout using a PanGestureRecognizer,
    // which (unlike SwipeGestureRecognizer) coexists with a ScrollView instead of losing to it.
    // Horizontal must beat vertical and clear a distance threshold to count as a tab swipe.
    public static class SwipeNav
    {
        // routeLeft  = where a swipe-RIGHT goes (content moves right -> previous tab)
        // routeRight = where a swipe-LEFT goes  (content moves left  -> next tab)
        public static void Attach(Layout root, string routeForSwipeRight, string routeForSwipeLeft)
        {
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

                        // Must be a mostly-horizontal drag of real distance.
                        if (Math.Abs(totalX) > 60 && Math.Abs(totalX) > Math.Abs(totalY) * 1.5)
                        {
                            fired = true;
                            string route = totalX > 0 ? routeForSwipeRight : routeForSwipeLeft;
                            if (!string.IsNullOrEmpty(route))
                                await Shell.Current.GoToAsync(route);
                        }
                        break;
                }
            };

            root.GestureRecognizers.Add(pan);
        }
    }
}