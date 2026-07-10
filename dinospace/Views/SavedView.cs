using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The Saved tab: everything bookmarked, newest first.
    public class SavedView : ContentView, ITabView
    {
        private VerticalStackLayout _list = null!;

        public SavedView() => Build();

        public void OnSelected() => Refresh();

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18, 16, 18, 8) };

            if (AppLayout.Playful)
                stack.Add(new Label
                {
                    Text = "saved",
                    FontFamily = Ui.Display, FontSize = Ui.S(32), TextColor = Theme.TextPrimary,
                    HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 2, 0, 0)
                });
            else
                stack.Add(new Label
                {
                    Text = "Your favourites are\nall right here.",
                    FontFamily = Ui.Display,
                    FontSize = Ui.S(26),
                    LineHeight = 1.12,
                    TextColor = Theme.TextSecondary
                });

            _list = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(18, 4, 18, 20) };

            var root = new VerticalStackLayout { Spacing = 0 };
            root.Add(stack);
            root.Add(_list);

            Content = new ScrollView { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            Refresh();
        }

        private void Refresh()
        {
            _list.Children.Clear();

            var dinos = SavedStore.Dinos;
            var space = SavedStore.Space;

            if (dinos.Count == 0 && space.Count == 0)
            {
                var empty = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(10, 40), HorizontalOptions = LayoutOptions.Center };
                // the sleepy mascot's spot (mascot_empty.png); a thought
                // bubble sticker holds the space until the art lands
                empty.Add(Ui.Mascot("mascot_empty", 110));
                empty.Add(new Label
                {
                    Text = "Nothing saved yet.",
                    FontFamily = Ui.Display, FontSize = Ui.S(20), TextColor = Theme.TextPrimary,
                    HorizontalTextAlignment = TextAlignment.Center
                });
                empty.Add(new Label
                {
                    Text = "Tap the bookmark on any creature or space object and it'll wait for you here.",
                    FontFamily = Ui.Fonts, FontSize = Ui.S(14), LineHeight = 1.45, TextColor = Theme.TextSecondary,
                    HorizontalTextAlignment = TextAlignment.Center
                });
                _list.Add(empty);
                return;
            }

            foreach (var name in dinos)
            {
                var d = DinoData.ByName(name);
                if (d == null) continue;
                var dd = d;
                _list.Add(EntryCards.ListRow(d.ImageFile, d.Name, $"{d.Diet} · {d.Era}", async () => await Nav.OpenDino(dd), goldStar: true));
            }
            foreach (var name in space)
            {
                var s = SpaceData.ByName(name);
                if (s == null) continue;
                var ss = s;
                _list.Add(EntryCards.ListRow(s.ImageFile, s.Name, $"{s.TypeLabel} · {s.Category}", async () => await Nav.OpenSpace(ss), goldStar: true));
            }
        }
    }
}
