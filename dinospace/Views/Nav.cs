using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Navigation helpers shared by every screen. Guards against the classic
    // double-tap-pushes-two-pages bug and gives every pushed page the same
    // deep-space gradient backdrop.
    public static class Nav
    {
        private static DateTime _lastPush = DateTime.MinValue;

        public static async Task Push(Page page, bool animated = true)
        {
            if (page == null) return;
            if ((DateTime.Now - _lastPush).TotalMilliseconds < 350) return; // debounce
            _lastPush = DateTime.Now;
            try { await Shell.Current.Navigation.PushAsync(page, animated); } catch { }
        }

        public static async Task OpenDino(Dinosaur d) => await Push(new DinoDetailPage(d));
        public static async Task OpenSpace(SpaceObject s) => await Push(new SpaceDetailPage(s));

        // `content` under a slim top bar (back arrow + small serif title).
        // Used by every pushed utility page.
        public static View DetailScaffold(string title, View content, Color accent, out ScrollView scroll)
        {
            var back = Ui.Icon(Ui.IconBack, 24, Theme.TextPrimary);
            back.Padding = new Thickness(6, 8, 14, 8);
            Ui.OnTap(back, async (_, _) =>
            {
                try { if (Shell.Current.Navigation.NavigationStack.Count > 1) await Shell.Current.Navigation.PopAsync(); } catch { }
            }, haptic: false);
            Ui.Describe(back, "Go back");

            var titleLabel = new Label
            {
                Text = title,
                FontFamily = Ui.Display,
                FontSize = 19,
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            var bar = new Grid { Padding = new Thickness(10, 6, 16, 6), ColumnSpacing = 2 };
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            bar.Add(back, 0, 0);
            bar.Add(titleLabel, 1, 0);

            scroll = new ScrollView { Content = content };

            var root = new Grid { RowSpacing = 0 };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            root.Add(bar, 0, 0);
            root.Add(scroll, 0, 1);
            return root;
        }
    }
}
