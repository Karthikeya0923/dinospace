using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // One record per card so a virtualized, two-column CollectionView can show
    // dinosaurs and space objects together without stutter.
    public class EntryItem
    {
        public string Image { get; init; } = "";
        public string Title { get; init; } = "";
        public string Chip { get; init; } = "";
        public string Subtitle { get; init; } = "";
        public Color Accent { get; init; } = Theme.AccentDino;
        public Color AccentDim { get; init; } = Theme.AccentDino;
        public object Data { get; init; } = null!;
    }

    // The encyclopedia browser: search, a Dino/Space/All segment, category
    // filters, and a fast two-column card grid. Replaces the old separate
    // DinoPedia / SpacePedia list pages with one visual, consistent screen.
    public class ExploreView : ContentView, ITabView
    {
        private static int _pendingSegment = 0;
        public static void RequestSegment(int s) => _pendingSegment = s;

        private readonly ObservableCollection<EntryItem> _items = new();
        private int _segment = 0;
        private string _category = "";
        private string _search = "";
        private SearchBar _searchBar = null!;
        private CollectionView _grid = null!;
        private HorizontalStackLayout _segStack = null!;
        private HorizontalStackLayout _catStack = null!;
        private Label _countLabel = null!;

        public ExploreView() => Build();

        public void OnSelected()
        {
            if (_pendingSegment != _segment)
            {
                _segment = _pendingSegment;
                _category = "";
                SyncSegmentUi();
                BuildCategoryChips();
            }
            _pendingSegment = _segment;
            Refresh();
        }

        private void Build()
        {
            var header = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(16, 18, 16, 8) };

            header.Add(new Label { Text = "Explore", FontFamily = Ui.Display, FontSize = 28, TextColor = Theme.TextPrimary });

            _searchBar = new SearchBar { Placeholder = "Search dinosaurs, planets, stars...", BackgroundColor = Colors.Transparent };
            _searchBar.TextChanged += (_, e) => { _search = e.NewTextValue ?? ""; Refresh(); };
            header.Add(new Border
            {
                Content = _searchBar,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(4, 0)
            });

            _segStack = new HorizontalStackLayout { Spacing = 8 };
            _segStack.Add(SegPill("All", 0));
            _segStack.Add(SegPill("Dinosaurs", 1));
            _segStack.Add(SegPill("Space", 2));
            header.Add(_segStack);

            _catStack = new HorizontalStackLayout { Spacing = 8 };
            header.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _catStack });

            _countLabel = new Label { FontFamily = Ui.Fonts, FontSize = 12, TextColor = Theme.TextHint };
            header.Add(_countLabel);

            _grid = new CollectionView
            {
                ItemsSource = _items,
                SelectionMode = SelectionMode.Single,
                ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical) { HorizontalItemSpacing = 12, VerticalItemSpacing = 12 },
                ItemTemplate = new DataTemplate(CardTemplate),
                VerticalScrollBarVisibility = ScrollBarVisibility.Never
            };
            _grid.SelectionChanged += OnCardSelected;

            _grid.Margin = new Thickness(16, 4, 16, 16);
            var root = new Grid { RowSpacing = 0 };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            root.Add(header, 0, 0);
            root.Add(_grid, 0, 1);
            Content = root;

            SyncSegmentUi();
            BuildCategoryChips();
            Refresh();
        }

        private Border SegPill(string text, int index)
        {
            var label = new Label { Text = text, FontFamily = Ui.Fonts, FontSize = 13.5, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary };
            var pill = new Border
            {
                Content = label,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 13 },
                Padding = new Thickness(14, 8)
            };
            Ui.OnTap(pill, (_, _) =>
            {
                if (_segment == index) return;
                _segment = index; _pendingSegment = index; _category = "";
                SyncSegmentUi(); BuildCategoryChips(); Refresh();
            });
            pill.BindingContext = new SegRef(label, index);
            return pill;
        }

        private record SegRef(Label Label, int Index);

        private void SyncSegmentUi()
        {
            foreach (var child in _segStack.Children)
                if (child is Border b && b.BindingContext is SegRef r)
                {
                    bool on = r.Index == _segment;
                    Color accent = r.Index == 2 ? Theme.AccentSpace : r.Index == 1 ? Theme.AccentDino : Theme.AccentNova;
                    b.BackgroundColor = on ? Ui.MultiplyAlpha(accent, 0.18f) : Theme.Surface;
                    b.Stroke = on ? accent : Theme.HairlineSoft;
                    r.Label.TextColor = on ? accent : Theme.TextSecondary;
                }
        }

        private void BuildCategoryChips()
        {
            _catStack.Children.Clear();
            string[] cats = _segment switch
            {
                1 => new[] { "All", "Land", "Sea", "Flying", "Carnivore", "Herbivore" },
                2 => new[] { "All", "Solar System", "Stars", "Deep Space", "Exploration" },
                _ => new[] { "All" }
            };
            foreach (var c in cats) _catStack.Add(CatChip(c));
            _catStack.IsVisible = cats.Length > 1;
        }

        private Border CatChip(string text)
        {
            bool active = (_category == "" && text == "All") || _category == text;
            var label = new Label { Text = text, FontFamily = Ui.Fonts, FontSize = 12.5, TextColor = active ? Theme.TextOnAccent : Theme.ChipText };
            var chip = new Border
            {
                Content = label,
                BackgroundColor = active ? Theme.AccentNova : Theme.ChipBg,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(12, 6)
            };
            Ui.OnTap(chip, (_, _) => { _category = text == "All" ? "" : text; BuildCategoryChips(); Refresh(); });
            return chip;
        }

        // A tall image card. Border clips the image's top corners for free.
        private View CardTemplate()
        {
            var img = new Image { Aspect = Aspect.AspectFill, HeightRequest = 124 };
            img.SetBinding(Image.SourceProperty, new Binding(nameof(EntryItem.Image)));

            var chipLabel = new Label { FontFamily = Ui.Fonts, FontSize = 10.5, FontAttributes = FontAttributes.Bold };
            chipLabel.SetBinding(Label.TextProperty, new Binding(nameof(EntryItem.Chip)));
            chipLabel.SetBinding(Label.TextColorProperty, new Binding(nameof(EntryItem.Accent)));
            var chip = new Border
            {
                Content = chipLabel,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(7, 2),
                HorizontalOptions = LayoutOptions.Start
            };
            chip.SetBinding(Border.BackgroundColorProperty, new Binding(nameof(EntryItem.AccentDim)));

            var name = new Label { FontFamily = Ui.Display, FontSize = 15.5, TextColor = Theme.TextPrimary, LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 };
            name.SetBinding(Label.TextProperty, new Binding(nameof(EntryItem.Title)));

            var sub = new Label { FontFamily = Ui.Fonts, FontSize = 11.5, TextColor = Theme.TextSecondary, LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 2 };
            sub.SetBinding(Label.TextProperty, new Binding(nameof(EntryItem.Subtitle)));

            var info = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(11, 10, 11, 12) };
            info.Add(chip); info.Add(name); info.Add(sub);

            var imgWrap = new Grid { BackgroundColor = Theme.ImgPlaceholder, HeightRequest = 124 };
            imgWrap.Add(img);

            var col = new Grid { RowSpacing = 0 };
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            col.Add(imgWrap, 0, 0);
            col.Add(info, 0, 1);

            return new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = 0,
                HeightRequest = 218
            };
        }

        private async void OnCardSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not EntryItem item) return;
            _grid.SelectedItem = null;
            AppSettings.Tap();
            if (item.Data is Dinosaur d) await Nav.OpenDino(d);
            else if (item.Data is SpaceObject s) await Nav.OpenSpace(s);
        }

        private void Refresh()
        {
            string q = Retriever.Normalize(_search);
            var results = new List<EntryItem>();

            if (_segment is 0 or 1)
                foreach (var d in DinoData.All)
                {
                    if (_segment == 1 && !MatchesDinoCategory(d)) continue;
                    if (!MatchesSearch(q, d.Name, d.ShortDescription, d.Group, d.Aliases)) continue;
                    results.Add(new EntryItem
                    {
                        Image = d.ImageFile, Title = d.Name, Chip = d.Diet.ToUpperInvariant(), Subtitle = d.ShortDescription,
                        Accent = Theme.AccentDino, AccentDim = Ui.MultiplyAlpha(Theme.AccentDino, 0.16f), Data = d
                    });
                }

            if (_segment is 0 or 2)
                foreach (var s in SpaceData.All)
                {
                    if (_segment == 2 && _category != "" && s.Category != _category) continue;
                    if (!MatchesSearch(q, s.Name, s.ShortDescription, s.TypeLabel, s.Aliases)) continue;
                    results.Add(new EntryItem
                    {
                        Image = s.ImageFile, Title = s.Name, Chip = s.TypeLabel.ToUpperInvariant(), Subtitle = s.ShortDescription,
                        Accent = Theme.AccentSpace, AccentDim = Ui.MultiplyAlpha(Theme.AccentSpace, 0.16f), Data = s
                    });
                }

            _items.Clear();
            foreach (var r in results) _items.Add(r);
            _countLabel.Text = results.Count == 1 ? "1 entry" : $"{results.Count} entries";
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

        private static bool MatchesSearch(string q, string name, string desc, string group, string[] aliases)
        {
            if (string.IsNullOrEmpty(q)) return true;
            string hay = Retriever.Normalize($"{name} {desc} {group} {string.Join(' ', aliases)}");
            return hay.Contains(q);
        }
    }
}
