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

        // A ScrollView of `content` on the gradient background, with a slim
        // top bar (back chevron + title). Used by every pushed detail page.
        public static View DetailScaffold(string title, View content, Color accent, out ScrollView scroll)
        {
            var back = new Label
            {
                Text = "‹",
                FontSize = 34,
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center,
                Padding = new Thickness(4, 0, 12, 0)
            };
            Ui.OnTap(back, async (_, _) =>
            {
                try { if (Shell.Current.Navigation.NavigationStack.Count > 1) await Shell.Current.Navigation.PopAsync(); } catch { }
            });

            var titleLabel = new Label
            {
                Text = title,
                FontFamily = Ui.Display,
                FontSize = 18,
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            var bar = new Grid { Padding = new Thickness(8, 8, 16, 6), ColumnSpacing = 2 };
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
