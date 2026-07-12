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
        // First letter, shown on the night-sky stand-in until real art loads.
        public string Initial => Title.Length > 0 ? Title[..1].ToUpperInvariant() : "•";

        // Set when this row is a user drawing. Drawings are transparent
        // PNGs, so they AspectFit straight onto the row with nothing behind;
        // built-in art fills the square as usual.
        public string DrawingBg { get; init; } = "";
        public Aspect ThumbAspect => DrawingBg.Length == 0 ? Aspect.AspectFill : Aspect.AspectFit;
        public Color ThumbBg => DrawingBg.Length == 0 ? Color.FromArgb("#111527") : Colors.Transparent;
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
                Text = "encyclopedia",
                FontFamily = Ui.Display, FontSize = Ui.S(32), TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 2, 0, 0)
            });

            _entry = new Entry { Placeholder = Ui.T("Search creatures, planets, stars…"), BackgroundColor = Colors.Transparent, TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint, ReturnType = ReturnType.Search };
            _entry.TextChanged += (_, e) => { _query = e.NewTextValue ?? ""; Refresh(); };
            var glass = Ui.Icon(Ui.IconSearch, 22);
            glass.VerticalOptions = LayoutOptions.Center;
            var field = new Grid { ColumnSpacing = 8, Padding = new Thickness(14, 0) };
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            field.Add(glass, 0, 0);
            field.Add(_entry, 1, 0);
            header.Add(new Border
            {
                Content = field, BackgroundColor = Theme.Surface, Stroke = Theme.CardStroke, StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, MinimumHeightRequest = 52
            });

            _segments = new HorizontalStackLayout { Spacing = AppLayout.Playful ? 8 : 22 };
            _segments.Add(SegItem(Ui.T("All"), 0));
            _segments.Add(SegItem(Ui.T("Dinosaurs"), 1));
            _segments.Add(SegItem(Ui.T("Space"), 2));
            header.Add(_segments);

            _count = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint };
            header.Add(_count);

            _list = new CollectionView
            {
                ItemsSource = _rows,
                SelectionMode = SelectionMode.Single,
                ItemTemplate = new DataTemplate(RowTemplate),
                // Uniform rows: measure one, not all — snappier segment switches.
                ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
                VerticalScrollBarVisibility = ScrollBarVisibility.Never,
                Margin = new Thickness(18, 0, 18, 12)
            };
            _list.SelectionChanged += OnSelectedRow;

            var root = new Grid { RowSpacing = 0 };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            root.Add(header, 0, 0);
            root.Add(_list, 0, 1);
            Content = Ui.CapWidth(root);

            SyncSegments();
            Refresh();
        }

        private View SegItem(string text, int index)
        {
            if (AppLayout.Playful)
            {
                var lbl = new Label
                {
                    Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), FontAttributes = FontAttributes.Bold,
                    TextColor = Theme.ChipText, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center
                };
                var pill = new Border
                {
                    Content = lbl, BackgroundColor = Theme.ChipBg, Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 100 }, Padding = new Thickness(16, 9)
                };
                pill.BindingContext = (lbl, (View)pill, index, true);
                Ui.OnTap(pill, async (_, _) => { _segment = index; SyncSegments(); await System.Threading.Tasks.Task.Yield(); Refresh(); });
                return pill;
            }

            var label = new Label
            {
                Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(14), FontAttributes = FontAttributes.Bold,
                TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center
            };
            var underline = new BoxView { HeightRequest = 2.5, Color = Colors.Transparent, Margin = new Thickness(0, 5, 0, 0) };
            var col = new VerticalStackLayout { Spacing = 0, Children = { label, underline } };
            col.BindingContext = (label, (View)underline, index, false);
            // Underline moves immediately; the list swap follows a frame later.
            Ui.OnTap(col, async (_, _) => { _segment = index; SyncSegments(); await System.Threading.Tasks.Task.Yield(); Refresh(); });
            return col;
        }

        private void SyncSegments()
        {
            foreach (var child in _segments.Children)
                if (child is BindableObject bo && bo.BindingContext is ValueTuple<Label, View, int, bool> t)
                {
                    bool on = t.Item3 == _segment;
                    if (t.Item4) // playful pill
                    {
                        ((Border)t.Item2).BackgroundColor = on ? Theme.Accent : Theme.ChipBg;
                        t.Item1.TextColor = on ? Theme.TextOnAccent : Theme.ChipText;
                    }
                    else
                    {
                        t.Item1.TextColor = on ? Theme.TextPrimary : Theme.TextSecondary;
                        ((BoxView)t.Item2).Color = on ? Theme.Accent : Colors.Transparent;
                    }
                }
        }

        // Title -> a stable bright colour, so the playful list reads like a
        // rainbow instead of a wall of grey rows.
        private sealed class NameHueConverter : IValueConverter
        {
            public object Convert(object? value, Type t, object? p, System.Globalization.CultureInfo c)
                => value is string s ? PlayfulKit.HueFor(s) : Theme.Accent;
            public object ConvertBack(object? value, Type t, object? p, System.Globalization.CultureInfo c)
                => throw new NotSupportedException();
        }
        private static readonly NameHueConverter _hue = new();

        private View RowTemplate()
        {
            if (AppLayout.Playful) return PlayfulRowTemplate();

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

            var chevron = Ui.Icon(Ui.IconChevron, 22);
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

        // Playful search row: rounded pill card with a bright, per-item round
        // thumbnail. Kept in the CollectionView so it stays virtualized.
        private View PlayfulRowTemplate()
        {
            var img = new Image { Aspect = Aspect.AspectFill, WidthRequest = 58, HeightRequest = 58 };
            img.SetBinding(Image.SourceProperty, new Binding(nameof(EntryRow.Image)));
            var initial = new Label { FontFamily = Ui.Display, FontSize = 22, TextColor = Colors.White.WithAlpha(0.92f), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            initial.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Initial)));
            var thumbGrid = new Grid();
            thumbGrid.Add(initial);
            thumbGrid.Add(img);
            var thumb = new Border { Content = thumbGrid, WidthRequest = 58, HeightRequest = 58, Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 18 } };
            thumb.SetBinding(Border.BackgroundColorProperty, new Binding(nameof(EntryRow.Title), converter: _hue));

            var title = new Label { FontFamily = Ui.Display, FontSize = Ui.S(17.5), TextColor = Theme.TextPrimary };
            title.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Title)));
            var meta = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextSecondary, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };
            meta.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Meta)));
            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(title); info.Add(meta);

            var chevron = Ui.Icon(Ui.IconChevron, 22);
            chevron.VerticalOptions = LayoutOptions.Center;

            var row = new Grid { ColumnSpacing = 13, Padding = new Thickness(10, 9) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Add(thumb, 0, 0); row.Add(info, 1, 0); row.Add(chevron, 2, 0);

            return new Border
            {
                Content = row, BackgroundColor = Theme.AccentSoft, Stroke = Theme.CardStroke, StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 20 }, Padding = 0,
                Margin = new Thickness(0, 5), Shadow = Theme.CardShadow()
            };
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
