using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // "View all" for one domain: serif title, rounded search, quiet category
    // filter, and a two-column card grid that never clips titles.
    public class BrowsePage : ContentPage
    {
        private readonly string _domain; // "Dinosaurs" | "Space"
        private string _query = "";
        private string _category = "";
        private VerticalStackLayout _grid = null!;
        private HorizontalStackLayout _chips = null!;
        private Label _count = null!;

        public BrowsePage(string domain)
        {
            _domain = domain;
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18, 4, 18, 8) };

            stack.Add(new Label
            {
                Text = _domain,
                FontFamily = Ui.Display,
                FontSize = Ui.S(30),
                TextColor = Theme.TextPrimary
            });

            // search field
            var entry = new Entry { Placeholder = $"Search {_domain.ToLowerInvariant()}…", BackgroundColor = Colors.Transparent };
            entry.TextChanged += (_, e) => { _query = e.NewTextValue ?? ""; Refresh(); };
            var glass = Ui.Icon(Ui.IconSearch, 22, Theme.TextHint);
            glass.VerticalOptions = LayoutOptions.Center;
            var field = new Grid { ColumnSpacing = 8, Padding = new Thickness(14, 0) };
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            field.Add(glass, 0, 0);
            field.Add(entry, 1, 0);
            stack.Add(new Border
            {
                Content = field,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.Hairline,
                StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                MinimumHeightRequest = 52
            });

            // category chips
            _chips = new HorizontalStackLayout { Spacing = 8 };
            BuildChips();
            stack.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _chips });

            _count = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint };
            stack.Add(_count);

            _grid = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(18, 4, 18, 24) };

            var content = new VerticalStackLayout { Spacing = 0 };
            content.Add(stack);
            content.Add(_grid);

            var body = Nav.DetailScaffold(_domain, new ScrollView { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Never }, Theme.Accent, out _);
            Content = new Grid { BackgroundColor = Theme.Bg, Children = { body } };
            Refresh();
        }

        private void BuildChips()
        {
            _chips.Children.Clear();
            string[] cats = _domain == "Dinosaurs"
                ? new[] { "All", "Land", "Sea", "Flying", "Carnivore", "Herbivore" }
                : new[] { "All", "Solar System", "Stars", "Deep Space", "Exploration" };
            foreach (var c in cats)
            {
                bool active = (_category == "" && c == "All") || _category == c;
                var chip = new Border
                {
                    Content = new Label
                    {
                        Text = c,
                        FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), FontAttributes = FontAttributes.Bold,
                        TextColor = active ? Theme.TextOnAccent : Theme.ChipText
                    },
                    BackgroundColor = active ? Theme.Accent : Theme.ChipBg,
                    Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 100 },
                    Padding = new Thickness(14, 7)
                };
                var cc = c;
                Ui.OnTap(chip, (_, _) => { _category = cc == "All" ? "" : cc; BuildChips(); Refresh(); }, haptic: false);
                _chips.Add(chip);
            }
        }

        private void Refresh()
        {
            string q = Retriever.Normalize(_query);
            var items = new List<(string, string, string, Action)>();

            if (_domain == "Dinosaurs")
            {
                foreach (var d in DinoData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (!MatchesDinoCategory(d)) continue;
                    if (!Match(q, d.Name, d.Aliases)) continue;
                    var dd = d;
                    items.Add((d.ImageFile, d.Name, d.Era, async () => await Nav.OpenDino(dd)));
                }
            }
            else
            {
                foreach (var s in SpaceData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (_category != "" && s.Category != _category) continue;
                    if (!Match(q, s.Name, s.Aliases)) continue;
                    var ss = s;
                    items.Add((s.ImageFile, s.Name, s.TypeLabel, async () => await Nav.OpenSpace(ss)));
                }
            }

            _grid.Children.Clear();
            _grid.Add(EntryCards.TwoColumn(items));
            _count.Text = items.Count == 1 ? "1 entry" : $"{items.Count} entries";
        }

        private bool MatchesDinoCategory(Dinosaur d)
        {
            if (_category == "") return true;
            return _category switch
            {
                "Carnivore" => d.Diet.Contains("Carnivore", StringComparison.OrdinalIgnoreCase),
                "Herbivore" => d.Diet.Contains("Herbivore", StringComparison.OrdinalIgnoreCase),
                _ => d.Category == _category
            };
        }

        // Names and nicknames only, same as the Search tab.
        private static bool Match(string q, string name, string[] aliases)
        {
            if (string.IsNullOrEmpty(q)) return true;
            return Retriever.Normalize($"{name} {string.Join(' ', aliases)}").Contains(q);
        }
    }
}
