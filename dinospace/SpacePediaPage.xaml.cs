namespace dinospace
{
    public partial class SpacePediaPage : ContentPage
    {
        private bool _isNavigating = false;
        private DateTime _lastNav = DateTime.MinValue;
        private bool _filtersOpen = false;
        private bool _built = false;

        private string _category = "All";
        private string _type = "All";

        public SpacePediaPage()
        {
            InitializeComponent();
            SwipeBack.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if ((DateTime.Now - _lastNav).TotalMilliseconds > 500)
                _isNavigating = false;

            if (!_built)
            {
                _built = true;
                // Wait for the push animation to finish before doing heavy build work,
                // so the slide-in isn't blocked and stutters.
                Dispatcher.Dispatch(async () =>
                {
                    await Task.Delay(280);
                    BuildFilterPanel();
                    UpdateFilterButton();
                    BuildList();
                });
            }
        }

        private void OnFilterToggle(object sender, EventArgs e)
        {
            _filtersOpen = !_filtersOpen;
            FilterPanel.IsVisible = _filtersOpen;
        }

        private void BuildFilterPanel()
        {
            FilterChips.Children.Clear();

            FilterChips.Children.Add(MakeDimension("Category", new[] { "All", "Solar System", "Stars", "Deep Space" }, _category, v => { _category = v; OnFilterChanged(); }));

            var types = new List<string> { "All" };
            types.AddRange(SpaceData.GetAll().Select(s => s.TypeLabel).Distinct().OrderBy(t => t));
            FilterChips.Children.Add(MakeDimension("Type", types.ToArray(), _type, v => { _type = v; OnFilterChanged(); }));
        }

        private void OnFilterChanged()
        {
            BuildFilterPanel();
            UpdateFilterButton();
            BuildList();
        }

        private void UpdateFilterButton()
        {
            int active = 0;
            if (_category != "All") active++;
            if (_type != "All") active++;
            FilterButton.Text = active == 0 ? "Filters" : $"Filters ({active})";
        }

        private View MakeDimension(string title, string[] options, string current, Action<string> onSelect)
        {
            var head = new Label { Text = title, FontSize = 12, FontAttributes = FontAttributes.Bold, FontFamily = "Baloo", TextColor = Theme.TextSecondary };

            var row = new HorizontalStackLayout { Spacing = 8 };
            foreach (var opt in options)
                row.Add(MakeChip(opt, opt == current, onSelect));

            var scroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = row };

            var box = new VerticalStackLayout { Spacing = 6 };
            box.Add(head);
            box.Add(scroll);
            return box;
        }

        private View MakeChip(string text, bool selected, Action<string> onSelect)
        {
            var label = new Label { Text = text, FontSize = 13, TextColor = selected ? Colors.Black : Theme.TextPrimary, VerticalOptions = LayoutOptions.Center };

            var chip = new Frame { Padding = new Thickness(12, 6), CornerRadius = 14, HasShadow = false, BorderColor = Colors.Transparent, BackgroundColor = selected ? Theme.Accent : Theme.ChipBg, Content = label };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => onSelect(text);
            chip.GestureRecognizers.Add(tap);
            return chip;
        }

        private void OnSearchCompleted(object sender, EventArgs e) => BuildList();
        private void OnSearchChanged(object sender, TextChangedEventArgs e) => BuildList();

        private void BuildList()
        {
            string query = SearchEntry.Text?.Trim().ToLower() ?? "";

            var items = SpaceData.GetAll()
                .Where(s => _category == "All" || s.Category == _category)
                .Where(s => _type == "All" || s.TypeLabel == _type)
                .Where(s => string.IsNullOrEmpty(query) || s.Name.ToLower().Contains(query))
                .OrderBy(s => s.Name)
                .ToList();

            ResultsList.ItemsSource = items;
        }

        private async void OnSpaceTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            if (sender is View v && v.BindingContext is SpaceObject s)
            {
                _isNavigating = true;
                _lastNav = DateTime.Now;
                await Shell.Current.Navigation.PushAsync(new SpaceDetailPage(s));
            }
        }
    }
}