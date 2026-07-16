using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The Saved tab: everything bookmarked, newest first. The + in the
    // header (and in the empty state) opens SavedAddPage — a searchable
    // list of the whole encyclopedia where tapping stars entries straight
    // into this tab.
    public class SavedView : ContentView, ITabView
    {
        private VerticalStackLayout _list = null!;

        public SavedView() => Build();

        public void OnSelected() => Refresh();

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18, 16, 18, 8) };

            // Title row: the tab's heading with a round + button on the
            // right that opens the add-search. The button is in the header
            // so it's reachable whether the list is full or empty.
            var title = AppLayout.Playful
                ? new Label
                {
                    Text = "saved",
                    FontFamily = Ui.Display, FontSize = Ui.S(32), TextColor = Theme.TextPrimary,
                    HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 2, 0, 0)
                }
                : new Label
                {
                    Text = "Your favourites are\nall right here.",
                    FontFamily = Ui.Display,
                    FontSize = Ui.S(26),
                    LineHeight = 1.12,
                    TextColor = Theme.TextSecondary
                };

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            if (AppLayout.Playful)
            {
                // Playful centres its title; the + floats at the right edge
                // without pushing the title off-centre.
                header.Add(title, 0, 0);
                Grid.SetColumnSpan(title, 2);
            }
            else
                header.Add(title, 0, 0);
            header.Add(AddButton(44), 1, 0);
            stack.Add(header);

            _list = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(18, 4, 18, 20) };

            var root = new VerticalStackLayout { Spacing = 0 };
            root.Add(stack);
            root.Add(_list);

            Content = new ScrollView { Content = Ui.CapWidth(root), VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            Refresh();
        }

        // A round accent + that opens the add-search page. Refresh runs when
        // the page closes so anything starred there appears immediately.
        private View AddButton(double size)
        {
            var btn = new Border
            {
                WidthRequest = size, HeightRequest = size,
                BackgroundColor = Theme.Accent,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End,
                Content = Ui.Icon(Ui.IconPlus, size * 0.5),
                Shadow = Theme.CardShadow()
            };
            Ui.OnTap(btn, async (_, _) => await Nav.Push(() => new SavedAddPage(Refresh)));
            Ui.Describe(btn, "Add entries to saved");
            return btn;
        }

        // Rebuilt from the store every time the tab is shown, so a star
        // toggled anywhere in the app is reflected the moment you come back.
        private void Refresh()
        {
            _list.Children.Clear();

            var dinos = SavedStore.Dinos;
            var space = SavedStore.Space;

            if (dinos.Count == 0 && space.Count == 0)
            {
                var empty = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(10, 40), HorizontalOptions = LayoutOptions.Center };
                // A big friendly + where the list will grow: the fastest way
                // from "nothing saved yet" to a first favourite.
                var big = new Border
                {
                    WidthRequest = 76, HeightRequest = 76,
                    BackgroundColor = Theme.Accent,
                    Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 38 },
                    HorizontalOptions = LayoutOptions.Center,
                    Content = Ui.Icon(Ui.IconPlus, 34),
                    Shadow = Theme.CardShadow()
                };
                Ui.OnTap(big, async (_, _) => await Nav.Push(() => new SavedAddPage(Refresh)));
                Ui.Describe(big, "Add entries to saved");
                empty.Add(big);
                empty.Add(new Label
                {
                    Text = "Nothing saved yet.",
                    FontFamily = Ui.Display, FontSize = Ui.S(20), TextColor = Theme.TextPrimary,
                    HorizontalTextAlignment = TextAlignment.Center
                });
                empty.Add(new Label
                {
                    Text = "Tap the star on any creature or space object — or the + right here — and it'll wait for you.",
                    FontFamily = Ui.Fonts, FontSize = Ui.S(14), LineHeight = 1.45, TextColor = Theme.TextSecondary,
                    HorizontalTextAlignment = TextAlignment.Center
                });
                _list.Add(empty);
                return;
            }

            // Bookmarks store only names; skip any that no longer resolve
            // (an entry renamed between versions just drops off the list).
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

    // Add-to-saved: a search field over the whole encyclopedia with a gold
    // star on every row. Tapping a row stars (or un-stars) it in place — no
    // popping back after each pick, so several favourites go in at once.
    // Names and nicknames only, so "megal" finds Megalodon and nothing else.
    public class SavedAddPage : ContentPage
    {
        private readonly Action _onChanged;
        private readonly ObservableCollection<EntryRow> _items = new();
        private Entry _entry = null!;

        public SavedAddPage(Action onChanged)
        {
            _onChanged = onChanged;
            Build();
            Filter("");
            SwipeBack.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Pop the keyboard right away — the page IS a search box.
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(350), () => _entry?.Focus());
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _onChanged();   // the Saved list refreshes the moment we're gone
        }

        private void Build()
        {
            // The same rounded search field as the encyclopedia tab, so the
            // muscle memory transfers: icon left, text fills, live filter.
            _entry = new Entry
            {
                Placeholder = Ui.T("Search creatures, planets, stars…"),
                BackgroundColor = Colors.Transparent,
                TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint,
                ReturnType = ReturnType.Search
            };
            _entry.TextChanged += (_, e) => Filter(e.NewTextValue ?? "");
            var glass = Ui.Icon(Ui.IconSearch, 22);
            glass.VerticalOptions = LayoutOptions.Center;
            var field = new Grid { ColumnSpacing = 8, Padding = new Thickness(14, 0) };
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            field.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            field.Add(glass, 0, 0);
            field.Add(_entry, 1, 0);
            var searchWrap = new Border
            {
                Content = field,
                BackgroundColor = Theme.Surface, Stroke = Theme.CardStroke, StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, MinimumHeightRequest = 52,
                Margin = new Thickness(16, 4, 16, 8)
            };

            var list = new CollectionView
            {
                ItemsSource = _items,
                SelectionMode = SelectionMode.None,   // rows own their taps
                ItemTemplate = new DataTemplate(RowTemplate),
                ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
                VerticalScrollBarVisibility = ScrollBarVisibility.Never,
                Margin = new Thickness(16, 0, 16, 16)
            };

            var col = new Grid { RowSpacing = 0 };
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            col.Add(searchWrap, 0, 0);
            col.Add(list, 0, 1);

            Content = Ui.PageRoot(Nav.DetailScaffoldFixed("Add to saved", col));
        }

        private View RowTemplate()
        {
            var initial = new Label
            {
                FontFamily = Ui.Display, FontSize = 18, TextColor = Color.FromArgb("#E3BE55"),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            };
            initial.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Initial)));
            var img = new FaceThumbView { WidthRequest = 46, HeightRequest = 46, FaceBg = Theme.SurfaceAlt };
            img.SetBinding(FaceThumbView.ImageNameProperty, new Binding(nameof(EntryRow.Image)));
            var thumbGrid = new Grid { BackgroundColor = Color.FromArgb("#111527") };
            thumbGrid.Add(initial);
            thumbGrid.Add(img);
            var thumb = new Border
            {
                Content = thumbGrid, WidthRequest = 46, HeightRequest = 46,
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 10 }
            };

            var name = new Label { FontFamily = Ui.Display, FontSize = Ui.S(16), TextColor = Theme.TextPrimary, VerticalOptions = LayoutOptions.Center };
            name.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Title)));
            var meta = new Label { FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center };
            meta.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Meta)));
            var text = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center, Children = { name, meta } };

            // The star mirrors the entry's live saved state; recycled rows
            // re-read it whenever their BindingContext changes.
            var star = new Ui.IconToggle(Ui.IconStarOutline, Ui.IconStar, 22);
            star.VerticalOptions = LayoutOptions.Center;

            var row = new Grid { ColumnSpacing = 12, Padding = new Thickness(2, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Add(thumb, 0, 0);
            row.Add(text, 1, 0);
            row.Add(star, 2, 0);

            row.BindingContextChanged += (_, _) =>
            {
                if (row.BindingContext is EntryRow r) star.Show(IsSaved(r));
            };
            Ui.OnTap(row, (_, _) =>
            {
                if (row.BindingContext is EntryRow r) star.Show(ToggleSaved(r));
            });
            return row;
        }

        private static bool IsSaved(EntryRow r) => r.Data switch
        {
            Dinosaur d => SavedStore.IsDinoSaved(d.Name),
            SpaceObject s => SavedStore.IsSpaceSaved(s.Name),
            _ => false
        };

        private static bool ToggleSaved(EntryRow r) => r.Data switch
        {
            Dinosaur d => SavedStore.ToggleDino(d.Name),
            SpaceObject s => SavedStore.ToggleSpace(s.Name),
            _ => false
        };

        // Same matching rule as the encyclopedia: names and nicknames only,
        // so a query narrows to exactly the entries it names.
        private void Filter(string query)
        {
            string q = Retriever.Normalize(query);
            _items.Clear();

            foreach (var d in DinoData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                if (Match(q, d.Name, d.Aliases))
                    _items.Add(new EntryRow { Image = d.ImageFile, Title = d.Name, Meta = $"{d.Diet} · {d.Era}", Data = d });
            foreach (var s in SpaceData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                if (Match(q, s.Name, s.Aliases))
                    _items.Add(new EntryRow { Image = s.ImageFile, Title = s.Name, Meta = $"{s.TypeLabel} · {s.Category}", Data = s });
        }

        private static bool Match(string q, string name, string[] aliases)
        {
            if (string.IsNullOrEmpty(q)) return true;
            return Retriever.Normalize($"{name} {string.Join(' ', aliases)}").Contains(q);
        }
    }
}
