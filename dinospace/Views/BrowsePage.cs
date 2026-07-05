using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // "View all" for one domain: serif title, rounded search, quiet category
    // filter, and a virtualized two-column card grid (fast, no clipping).
    public class BrowsePage : ContentPage
    {
        private readonly string _domain; // "Dinosaurs" | "Space"
        private readonly RangeObservableCollection<EntryRow> _items = new();
        private string _query = "";
        private string _category = "";
        private CollectionView _grid = null!;
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
            var header = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18, 4, 18, 8) };

            header.Add(new Label { Text = _domain, FontFamily = Ui.Display, FontSize = Ui.S(30), TextColor = Theme.TextPrimary });

            var entry = new Entry { Placeholder = $"Search {_domain.ToLowerInvariant()}…", BackgroundColor = Colors.Transparent, TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint };
            entry.TextChanged += (_, e) => { _query = e.NewTextValue ?? ""; Refresh(); };
            var glass = Ui.Icon(Ui.IconSearch, 22, Theme.TextHint);
            glass.VerticalOptions = LayoutOptions.Center;
            var field = new Grid { ColumnSpacing = 8, Padding = new Thickness(14, 0) };
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            field.Add(glass, 0, 0);
            field.Add(entry, 1, 0);
            header.Add(new Border
            {
                Content = field, BackgroundColor = Theme.Surface, Stroke = Theme.Hairline, StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, MinimumHeightRequest = 52
            });

            _chips = new HorizontalStackLayout { Spacing = 8 };
            BuildChips();
            header.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _chips });

            _count = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint };
            header.Add(_count);

            _grid = new CollectionView
            {
                ItemsSource = _items,
                SelectionMode = SelectionMode.Single,
                ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical) { HorizontalItemSpacing = 12, VerticalItemSpacing = 12 },
                ItemTemplate = new DataTemplate(CardTemplate),
                VerticalScrollBarVisibility = ScrollBarVisibility.Never,
                Margin = new Thickness(18, 4, 18, 16)
            };
            _grid.SelectionChanged += OnSelectedCard;

            var col = new Grid { RowSpacing = 0 };
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            col.Add(header, 0, 0);
            col.Add(_grid, 0, 1);

            var body = Nav.DetailScaffold("", col, Theme.Accent, out _);
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
                        Text = c, FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), FontAttributes = FontAttributes.Bold,
                        TextColor = active ? Theme.TextOnAccent : Theme.ChipText
                    },
                    BackgroundColor = active ? Theme.Accent : Theme.ChipBg,
                    Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 100 },
                    Padding = new Thickness(14, 7)
                };
                var cc = c;
                Ui.OnTap(chip, (_, _) => { _category = cc == "All" ? "" : cc; BuildChips(); Refresh(); }, haptic: false);
                _chips.Add(chip);
            }
        }

        private View CardTemplate()
        {
            var img = new Image { Aspect = Aspect.AspectFill, HeightRequest = 118 };
            img.SetBinding(Image.SourceProperty, new Binding(nameof(EntryRow.Image)));
            var imgWrap = new Grid { HeightRequest = 118, BackgroundColor = Theme.ImgPlaceholder };
            imgWrap.Add(img);

            var name = new Label { FontFamily = Ui.Display, FontSize = Ui.S(16.5), LineHeight = 1.1, MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation, TextColor = Theme.TextPrimary };
            name.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Title)));
            var meta = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12), MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation, TextColor = Theme.TextSecondary };
            meta.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Meta)));

            // Name and meta grouped under the image (no stretched gap), with a
            // fixed card height so two-line names never clip.
            var info = new VerticalStackLayout { Padding = new Thickness(12, 8, 12, 9), Spacing = 2, VerticalOptions = LayoutOptions.Start };
            info.Add(name); info.Add(meta);

            var colc = new VerticalStackLayout { Spacing = 0 };
            colc.Add(imgWrap); colc.Add(info);

            return new Border
            {
                Content = colc, BackgroundColor = Theme.Surface, Stroke = Theme.CardStroke, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = 0, HeightRequest = 190, Shadow = Theme.CardShadow()
            };
        }

        private async void OnSelectedCard(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not EntryRow r) return;
            _grid.SelectedItem = null;
            AppSettings.Tap();
            if (r.Data is Dinosaur d) await Nav.OpenDino(d);
            else if (r.Data is SpaceObject s) await Nav.OpenSpace(s);
        }

        private void Refresh()
        {
            string q = Retriever.Normalize(_query);
            var next = new List<EntryRow>();

            if (_domain == "Dinosaurs")
                foreach (var d in DinoData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (!MatchesDinoCategory(d)) continue;
                    if (!Match(q, d.Name, d.Aliases)) continue;
                    next.Add(new EntryRow { Image = d.ImageFile, Title = d.Name, Meta = d.Era, Data = d });
                }
            else
                foreach (var s in SpaceData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (_category != "" && s.Category != _category) continue;
                    if (!Match(q, s.Name, s.Aliases)) continue;
                    next.Add(new EntryRow { Image = s.ImageFile, Title = s.Name, Meta = s.TypeLabel, Data = s });
                }

            _items.ReplaceAll(next);
            _count.Text = next.Count == 1 ? "1 entry" : $"{next.Count} entries";
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

        private static bool Match(string q, string name, string[] aliases)
        {
            if (string.IsNullOrEmpty(q)) return true;
            return Retriever.Normalize($"{name} {string.Join(' ', aliases)}").Contains(q);
        }
    }
}
