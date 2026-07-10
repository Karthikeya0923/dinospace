using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // A searchable chooser over the WHOLE encyclopedia — dinosaurs and space
    // objects together. Used for building custom lists. Calls back with the
    // picked entry and pops itself.
    public class EntryPickerPage : ContentPage
    {
        private readonly Action<object> _onPick;
        private readonly ObservableCollection<EntryRow> _items = new();
        private CollectionView _list = null!;

        public EntryPickerPage(Action<object> onPick)
        {
            _onPick = onPick;
            Build();
            Filter("");
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var search = new SearchBar { Placeholder = "Search everything…", BackgroundColor = Colors.Transparent, TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint };
            search.TextChanged += (_, e) => Filter(e.NewTextValue ?? "");
            var searchWrap = new Border
            {
                Content = search,
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = new Thickness(4, 0),
                Margin = new Thickness(16, 4, 16, 8)
            };

            _list = new CollectionView
            {
                ItemsSource = _items,
                SelectionMode = SelectionMode.Single,
                ItemTemplate = new DataTemplate(RowTemplate),
                ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
                VerticalScrollBarVisibility = ScrollBarVisibility.Never,
                Margin = new Thickness(16, 0, 16, 16)
            };
            _list.SelectionChanged += OnSelected;

            var col = new Grid { RowSpacing = 0 };
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            col.Add(searchWrap, 0, 0);
            col.Add(_list, 0, 1);

            var content = Nav.DetailScaffoldFixed("Add to your list", col);
            Content = Ui.PageRoot(content);
        }

        private View RowTemplate()
        {
            var initial = new Label
            {
                FontFamily = Ui.Display, FontSize = 18, TextColor = Color.FromArgb("#E3BE55"),
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            };
            initial.SetBinding(Label.TextProperty, new Binding(nameof(EntryRow.Initial)));
            var img = new Image { WidthRequest = 46, HeightRequest = 46 };
            img.SetBinding(Image.SourceProperty, new Binding(nameof(EntryRow.Image)));
            img.SetBinding(Image.AspectProperty, new Binding(nameof(EntryRow.ThumbAspect)));
            var thumbGrid = new Grid();
            thumbGrid.SetBinding(Grid.BackgroundColorProperty, new Binding(nameof(EntryRow.ThumbBg)));
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

            var row = new Grid { ColumnSpacing = 12, Padding = new Thickness(2, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            row.Add(thumb, 0, 0);
            row.Add(text, 1, 0);
            return row;
        }

        private void Filter(string query)
        {
            string q = Retriever.Normalize(query);
            _items.Clear();

            // Your own creations come first — they're the reason you opened
            // this, and they mix into any list right alongside the built-ins.
            foreach (var c in CreationStore.All())
                if (q.Length == 0 || Retriever.Normalize(c.Name).Contains(q))
                    _items.Add(new EntryRow { Image = c.ImagePath, Title = c.Name, Meta = c.MetaLine, Data = c, DrawingBg = c.CanvasColor });

            foreach (var d in DinoData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                if (q.Length == 0 || Retriever.Normalize(d.Name).Contains(q))
                    _items.Add(new EntryRow { Image = d.ImageFile, Title = d.Name, Meta = "Dinosaur · " + d.Era, Data = d });
            foreach (var s in SpaceData.All.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                if (q.Length == 0 || Retriever.Normalize(s.Name).Contains(q))
                    _items.Add(new EntryRow { Image = s.ImageFile, Title = s.Name, Meta = "Space · " + s.TypeLabel, Data = s });
        }

        private async void OnSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not EntryRow r) return;
            _list.SelectedItem = null;
            AppSettings.Tap();
            _onPick(r.Data);
            try { await Shell.Current.Navigation.PopAsync(); } catch { }
        }
    }
}
