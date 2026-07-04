using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The Search tab: a big rounded search field, minimal All/Dinosaurs/Space
    // filter, and an alphabetical list of every entry that filters live.
    public class SearchView : ContentView, ITabView
    {
        private Entry _entry = null!;
        private VerticalStackLayout _results = null!;
        private HorizontalStackLayout _segments = null!;
        private Label _count = null!;
        private string _query = "";
        private int _segment; // 0 all, 1 dino, 2 space

        public SearchView() => Build();

        public void OnSelected() { }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18, 16, 18, 8) };

            stack.Add(new Label
            {
                Text = "Every creature and cosmos,\nall right here.",
                FontFamily = Ui.Display,
                FontSize = Ui.S(26),
                LineHeight = 1.12,
                TextColor = Theme.TextSecondary
            });

            // Rounded search field, reference-style.
            _entry = new Entry { Placeholder = "Search dinosaurs, planets, stars…", BackgroundColor = Colors.Transparent, ReturnType = ReturnType.Search };
            _entry.TextChanged += (_, e) => { _query = e.NewTextValue ?? ""; Refresh(); };

            var glass = Ui.Icon(Ui.IconSearch, 22, Theme.TextHint);
            glass.VerticalOptions = LayoutOptions.Center;

            var field = new Grid { ColumnSpacing = 8, Padding = new Thickness(14, 0) };
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            field.Add(glass, 0, 0);
            field.Add(_entry, 1, 0);

            stack.Add(new Border
            {
                Content = field,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.Hairline,
                StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                MinimumHeightRequest = 52
            });

            // Minimal text filter with red underline for the active one.
            _segments = new HorizontalStackLayout { Spacing = 22 };
            _segments.Add(SegItem("All", 0));
            _segments.Add(SegItem("Dinosaurs", 1));
            _segments.Add(SegItem("Space", 2));
            stack.Add(_segments);

            _count = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint };
            stack.Add(_count);

            _results = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(18, 0, 18, 20) };

            var root = new VerticalStackLayout { Spacing = 0 };
            root.Add(stack);
            root.Add(_results);

            Content = new ScrollView { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            SyncSegments();
            Refresh();
        }

        private View SegItem(string text, int index)
        {
            var label = new Label
            {
                Text = text,
                FontFamily = Ui.Fonts,
                FontSize = Ui.S(14),
                FontAttributes = FontAttributes.Bold,
                TextColor = Theme.TextSecondary,
                HorizontalTextAlignment = TextAlignment.Center
            };
            var underline = new BoxView { HeightRequest = 2.5, Color = Colors.Transparent, Margin = new Thickness(0, 5, 0, 0) };
            var col = new VerticalStackLayout { Spacing = 0, Children = { label, underline } };
            col.BindingContext = (label, underline, index);
            Ui.OnTap(col, (_, _) => { _segment = index; SyncSegments(); Refresh(); }, haptic: false);
            return col;
        }

        private void SyncSegments()
        {
            foreach (var child in _segments.Children)
                if (child is VerticalStackLayout col && col.BindingContext is ValueTuple<Label, BoxView, int> t)
                {
                    bool on = t.Item3 == _segment;
                    t.Item1.TextColor = on ? Theme.TextPrimary : Theme.TextSecondary;
                    t.Item2.Color = on ? Theme.Accent : Colors.Transparent;
                }
        }

        private void Refresh()
        {
            string q = Retriever.Normalize(_query);
            _results.Children.Clear();
            int n = 0;

            if (_segment is 0 or 1)
                foreach (var d in DinoData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (!Match(q, d.Name, d.ShortDescription, d.Group, d.Aliases)) continue;
                    var dd = d;
                    _results.Add(EntryCards.ListRow(d.ImageFile, d.Name, $"{d.Diet} · {d.Era}", async () => await Nav.OpenDino(dd)));
                    n++;
                }

            if (_segment is 0 or 2)
                foreach (var s in SpaceData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (!Match(q, s.Name, s.ShortDescription, s.TypeLabel, s.Aliases)) continue;
                    var ss = s;
                    _results.Add(EntryCards.ListRow(s.ImageFile, s.Name, $"{s.TypeLabel} · {s.Category}", async () => await Nav.OpenSpace(ss)));
                    n++;
                }

            _count.Text = n == 1 ? "1 entry" : $"{n} entries";
        }

        private static bool Match(string q, string name, string desc, string group, string[] aliases)
        {
            if (string.IsNullOrEmpty(q)) return true;
            return Retriever.Normalize($"{name} {desc} {group} {string.Join(' ', aliases)}").Contains(q);
        }
    }
}
