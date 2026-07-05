using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // A searchable, alphabetical creature chooser used by Dino Battle. Calls
    // back with the picked creature and pops itself.
    public class CreaturePickerPage : ContentPage
    {
        private readonly Action<Dinosaur> _onPick;
        private readonly ObservableCollection<Dinosaur> _items = new();
        private CollectionView _list = null!;

        public CreaturePickerPage(Action<Dinosaur> onPick)
        {
            _onPick = onPick;
            Build();
            Filter("");
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var search = new SearchBar { Placeholder = "Search creatures...", BackgroundColor = Colors.Transparent, TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint };
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
                VerticalScrollBarVisibility = ScrollBarVisibility.Never,
                Margin = new Thickness(16, 0, 16, 16)
            };
            _list.SelectionChanged += OnSelected;

            var col = new Grid { RowSpacing = 0 };
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            col.Add(searchWrap, 0, 0);
            col.Add(_list, 0, 1);

            var content = Nav.DetailScaffold("Choose a creature", col, Theme.AccentDino, out _);
            Content = Ui.PageRoot(content);
        }

        private View RowTemplate()
        {
            var img = new Image { Aspect = Aspect.AspectFill, WidthRequest = 48, HeightRequest = 48 };
            img.SetBinding(Image.SourceProperty, new Binding(nameof(Dinosaur.ImageFile)));
            var thumb = new Border
            {
                Content = img, WidthRequest = 48, HeightRequest = 48,
                BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }
            };

            var name = new Label { FontFamily = Ui.Display, FontSize = 16, TextColor = Theme.TextPrimary, VerticalOptions = LayoutOptions.Center };
            name.SetBinding(Label.TextProperty, new Binding(nameof(Dinosaur.Name)));
            var sub = new Label { FontFamily = Ui.Fonts, FontSize = 12, TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center };
            sub.SetBinding(Label.TextProperty, new Binding(nameof(Dinosaur.ShortDescription)));

            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(name); info.Add(sub);

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.Add(thumb, 0, 0); grid.Add(info, 1, 0);

            return new Border
            {
                Content = grid,
                BackgroundColor = Theme.Surface, Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 }, Padding = new Thickness(10), Margin = new Thickness(0, 4)
            };
        }

        private void Filter(string query)
        {
            string q = Retriever.Normalize(query);
            var results = DinoData.All
                .Where(d => q.Length == 0 || Retriever.Normalize($"{d.Name} {string.Join(' ', d.Aliases)}").Contains(q))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase);
            _items.Clear();
            foreach (var d in results) _items.Add(d);
        }

        private async void OnSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not Dinosaur d) return;
            _list.SelectedItem = null;
            AppSettings.Tap();
            _onPick(d);
            try { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); } catch { }
        }
    }
}
