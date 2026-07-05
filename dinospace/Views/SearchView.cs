using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // A row in the search list (dinosaur or space object).
    public class EntryRow
    {
        public string Image { get; init; } = "";
        public string Title { get; init; } = "";
        public string Meta { get; init; } = "";
        public object Data { get; init; } = null!;
    }

    // The Search tab: big rounded field, All/Dinosaurs/Space filter, and a
    // virtualized alphabetical list that filters live (fast even with every
    // entry loaded).
    public class SearchView : ContentView, ITabView
    {
        private readonly RangeObservableCollection<EntryRow> _rows = new();
        private Entry _entry = null!;
        private HorizontalStackLayout _segments = null!;
        private CollectionView _list = null!;
        private Label _count = null!;
        private string _query = "";
        private int _segment;

        public SearchView() => Build();

        public void OnSelected() { }

        private void Build()
        {
            var header = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18, 16, 18, 8) };

            header.Add(new Label
            {
                Text = "Every creature and cosmos,\nall right here.",
                FontFamily = Ui.Display, FontSize = Ui.S(26), LineHeight = 1.12, TextColor = Theme.TextSecondary
            });

            _entry = new Entry { Placeholder = "Search dinosaurs, planets, stars…", BackgroundColor = Colors.Transparent, TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint, ReturnType = ReturnType.Search };
            _entry.TextChanged += (_, e) => { _query = e.NewTextValue ?? ""; Refresh(); };
            var glass = Ui.Icon(Ui.IconSearch, 22, Theme.TextHint);
            glass.VerticalOptions = LayoutOptions.Center;
            var field = new Grid { ColumnSpacing = 8, Padding = new Thickness(14, 0) };
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            field.Add(glass, 0, 0);
            field.Add(_entry, 1, 0);
            header.Add(new Border
            {
                Content = field, BackgroundColor = Theme.Surface, Stroke = Theme.Hairline, StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, MinimumHeightRequest = 52
            });

            _segments = new HorizontalStackLayout { Spacing = 22 };
            _segments.Add(SegItem("All", 0));
            _segments.Add(SegItem("Dinosaurs", 1));
            _segments.Add(SegItem("Space", 2));
            header.Add(_segments);

            _count = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint };
            header.Add(_count);

            _list = new CollectionView
            {
                ItemsSource = _rows,
                SelectionMode = SelectionMode.Single,
                ItemTemplate = new DataTemplate(RowTemplate),
                VerticalScrollBarVisibility = ScrollBarVisibility.Never,
                Margin = new Thickness(18, 0, 18, 12)
            };
            _list.SelectionChanged += OnSelectedRow;

            var root = new Grid { RowSpacing = 0 };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            root.Add(header, 0, 0);
            root.Add(_list, 0, 1);
            Content = root;

            SyncSegments();
            Refresh();
        }

        private View SegItem(string text, int index)
        {
            var label = new Label
            {
                Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(14), FontAttributes = FontAttributes.Bold,
                TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center
            };
            var underline = new BoxView { HeightRequest = 2.5, Color = Colors.Transparent, Margin = new Thickness(0, 5, 0, 0) };
            var col = new VerticalStackLayout { Spacing = 0, Children = { label, underline } };
            col.BindingContext = (label, underline, index);
            Ui.OnTap(col, (_, _) => { _segment = index; SyncSegments(); Refresh(); });
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

        private View RowTemplate()
        {
            var img = new Image { Aspect = Aspect.AspectFill, WidthRequest = 54, HeightRequest = 54 };
            img.SetBinding(Image.SourceProperty, new Binding(nameof(EntryRow.Image)));
            var thumb = new Border
            {
                Content = img, WidthRequest = 54, HeightRequest = 54, BackgroundColor = Theme.ImgPlaceholder,
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 10 }
            };

            var title = new Label { FontFamily = Ui.Display, FontSize = Ui.S(17), TextColor = Theme.TextPrimary };
            title.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Title)));
            var meta = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextSecondary, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };
            meta.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Meta)));
            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(title); info.Add(meta);

            var chevron = Ui.Icon(Ui.IconChevron, 22, Theme.TextHint);
            chevron.VerticalOptions = LayoutOptions.Center;

            var row = new Grid { ColumnSpacing = 12, Padding = new Thickness(2, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Add(thumb, 0, 0); row.Add(info, 1, 0); row.Add(chevron, 2, 0);

            var wrap = new VerticalStackLayout { Spacing = 0 };
            wrap.Add(row);
            wrap.Add(new BoxView { HeightRequest = 1, Color = Theme.HairlineSoft, Margin = new Thickness(66, 0, 0, 0) });
            return wrap;
        }

        private async void OnSelectedRow(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not EntryRow r) return;
            _list.SelectedItem = null;
            AppSettings.Tap();
            if (r.Data is Dinosaur d) await Nav.OpenDino(d);
            else if (r.Data is SpaceObject s) await Nav.OpenSpace(s);
        }

        private void Refresh()
        {
            string q = Retriever.Normalize(_query);
            var next = new List<EntryRow>();

            if (_segment is 0 or 1)
                foreach (var d in DinoData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                    if (Match(q, d.Name, d.Aliases))
                        next.Add(new EntryRow { Image = d.ImageFile, Title = d.Name, Meta = $"{d.Diet} · {d.Era}", Data = d });

            if (_segment is 0 or 2)
                foreach (var s in SpaceData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                    if (Match(q, s.Name, s.Aliases))
                        next.Add(new EntryRow { Image = s.ImageFile, Title = s.Name, Meta = $"{s.TypeLabel} · {s.Category}", Data = s });

            _rows.ReplaceAll(next);
            _count.Text = next.Count == 1 ? "1 entry" : $"{next.Count} entries";
        }

        // Names and nicknames only, so "earth" finds Earth (not every mention).
        private static bool Match(string q, string name, string[] aliases)
        {
            if (string.IsNullOrEmpty(q)) return true;
            return Retriever.Normalize($"{name} {string.Join(' ', aliases)}").Contains(q);
        }
    }
}
