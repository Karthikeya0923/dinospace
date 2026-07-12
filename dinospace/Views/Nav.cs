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

        // Forward navigation is instant (no slide) so it feels immediate.
        //
        // The destination is built through a factory and we yield one frame
        // first: that lets the tapped control's press animation paint before
        // the (sometimes heavy) page is constructed, so opening "View all", a
        // detail page, or the battle picker feels instant instead of stuttering
        // while the page builds on the tap thread.
        public static async Task Push(Func<Page> build, bool animated = false)
        {
            if (build == null) return;
            if ((DateTime.Now - _lastPush).TotalMilliseconds < 300) return; // debounce
            _lastPush = DateTime.Now;

            await Task.Yield();
            Page page;
            try { page = build(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nav build: " + ex); return; }
            if (page == null) return;
            try { await Shell.Current.Navigation.PushAsync(page, animated); } catch { }
        }

        // Convenience overload for callers that already hold a page instance.
        public static Task Push(Page page, bool animated = false) => Push(() => page, animated);

        public static Task OpenDino(Dinosaur d) => Push(() => new DinoDetailPage(d));
        public static Task OpenSpace(SpaceObject s) => Push(() => new SpaceDetailPage(s));

        // `content` under a slim top bar (back arrow + small serif title).
        // Used by every pushed utility page.
        public static View DetailScaffold(string title, View content, Color accent, out ScrollView scroll)
            => Scaffold(title, content, wrapInScroll: true, out scroll);

        // Same top bar, but the content manages its own scrolling. Lists MUST
        // use this one: a CollectionView inside a ScrollView loses its
        // virtualization and builds every single card up front — that was the
        // whole reason "View all" opened slowly.
        public static View DetailScaffoldFixed(string title, View content)
            => Scaffold(title, content, wrapInScroll: false, out _);

        private static View Scaffold(string title, View content, bool wrapInScroll, out ScrollView scroll)
        {
            var backIcon = Ui.Icon(Ui.IconBack, 24);
            var back = new Border
            {
                Content = backIcon,
                BackgroundColor = Colors.Transparent,
                Stroke = Colors.Transparent,
                WidthRequest = 48, HeightRequest = 44
            };
            Ui.OnTap(back, async (_, _) =>
            {
                try { if (Shell.Current.Navigation.NavigationStack.Count > 1) await Shell.Current.Navigation.PopAsync(); } catch { }
            });
            Ui.Describe(back, "Go back");

            // One compact row right at the top: back arrow left, the page's
            // big lowercase storybook title dead centre — no dead space.
            var bar = new Grid { Padding = new Thickness(8, 2, 8, 0), ColumnSpacing = 0 };
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            bar.Add(back, 0, 0);
            if (!string.IsNullOrEmpty(title))
                bar.Add(new Label
                {
                    Text = Ui.T(title),
                    FontFamily = Ui.Display,
                    FontSize = Ui.S(26),
                    TextColor = Theme.TextPrimary,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.TailTruncation
                }, 1, 0);

            scroll = new ScrollView { Content = wrapInScroll ? Ui.CapWidth(content) : null };

            var root = new Grid { RowSpacing = 0 };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            root.Add(bar, 0, 0);
            root.Add(wrapInScroll ? scroll : Ui.CapWidth(content), 0, 1);
            return root;
        }
    }
}
