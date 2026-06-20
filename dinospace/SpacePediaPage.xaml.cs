namespace dinospace
{
    public partial class SpacePediaPage : ContentPage
    {
        // Navigation guard to prevent double-taps opening two detail pages
        private bool _isNavigating = false;
        private DateTime _lastNav = DateTime.MinValue;
        private bool _filtersOpen = false;

        // Active filter state — "All" means no filter applied for that dimension
        private string _category = "All";
        private string _type = "All";

        public SpacePediaPage()
        {
            InitializeComponent();
            BuildFilterPanel();
            UpdateFilterButton();
            BuildList();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Release nav guard only after a real return (not a spurious re-appear from pushing a detail page)
            if ((DateTime.Now - _lastNav).TotalMilliseconds > 500)
                _isNavigating = false;
        }

        // ===== Filter panel =====

        // Toggle filter panel visibility
        private void OnFilterToggle(object sender, EventArgs e)
        {
            _filtersOpen = !_filtersOpen;
            FilterPanel.IsVisible = _filtersOpen;
        }

        // Rebuild all filter chip rows from current filter state
        private void BuildFilterPanel()
        {
            FilterChips.Children.Clear();

            FilterChips.Children.Add(MakeDimension(
                "Category",
                new[] { "All", "Solar System", "Stars", "Deep Space" },
                _category,
                v => { _category = v; OnFilterChanged(); }));

            // Type options are derived dynamically from the data
            var types = new List<string> { "All" };
            types.AddRange(SpaceData.GetAll().Select(s => s.TypeLabel).Distinct().OrderBy(t => t));
            FilterChips.Children.Add(MakeDimension(
                "Type",
                types.ToArray(),
                _type,
                v => { _type = v; OnFilterChanged(); }));
        }

        // Called whenever any filter value changes
        private void OnFilterChanged()
        {
            BuildFilterPanel();
            UpdateFilterButton();
            BuildList();
        }

        // Show active filter count on the Filters button (e.g. "Filters (1)")
        private void UpdateFilterButton()
        {
            int active = 0;
            if (_category != "All") active++;
            if (_type != "All") active++;
            FilterButton.Text = active == 0 ? "Filters" : $"Filters ({active})";
        }

        // Build one filter row: a label heading + horizontally scrollable chip strip
        private View MakeDimension(string title, string[] options, string current, Action<string> onSelect)
        {
            var head = new Label { Text = title, FontSize = 12, FontAttributes = FontAttributes.Bold, FontFamily = "Baloo", TextColor = Theme.TextSecondary };

            var row = new HorizontalStackLayout { Spacing = 8 };
            foreach (var opt in options)
                row.Add(MakeChip(opt, opt == current, onSelect));

            var scroll = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
                Content = row
            };

            var box = new VerticalStackLayout { Spacing = 6 };
            box.Add(head);
            box.Add(scroll);
            return box;
        }

        // Build a single selectable chip; selected chips use Accent color with dark text
        private View MakeChip(string text, bool selected, Action<string> onSelect)
        {
            var label = new Label
            {
                Text = text,
                FontSize = 13,
                TextColor = selected ? Colors.Black : Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center
            };

            var chip = new Frame
            {
                Padding = new Thickness(12, 6),
                CornerRadius = 14,
                HasShadow = false,
                BorderColor = Colors.Transparent,
                BackgroundColor = selected ? Theme.Accent : Theme.ChipBg,
                Content = label
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => onSelect(text);
            chip.GestureRecognizers.Add(tap);
            return chip;
        }

        // ===== Search + list =====

        private void OnSearchCompleted(object sender, EventArgs e) => BuildList();
        private void OnSearchChanged(object sender, TextChangedEventArgs e) => BuildList();

        // Filter and display the space object list based on active filters and search query
        private void BuildList()
        {
            ResultsStack.Children.Clear();
            string query = SearchEntry.Text?.Trim().ToLower() ?? "";

            var items = SpaceData.GetAll()
                .Where(s => _category == "All" || s.Category == _category)
                .Where(s => _type == "All" || s.TypeLabel == _type)
                .Where(s => string.IsNullOrEmpty(query) || s.Name.ToLower().Contains(query))
                .OrderBy(s => s.Name)
                .ToList();

            if (items.Count == 0)
            {
                ResultsStack.Children.Add(new Label
                {
                    Text = "Nothing matches these filters",
                    TextColor = Theme.TextSecondary,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 16)
                });
                return;
            }

            foreach (var s in items)
                ResultsStack.Children.Add(UiHelpers.BuildSpaceRow(s, OnSpaceTapped));
        }

        private async void OnSpaceTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            if (sender is View v && v.BindingContext is SpaceObject s)
            {
                _isNavigating = true;
                _lastNav = DateTime.Now;
                await Shell.Current.Navigation.PushAsync(new SpaceDetailPage(s), false);
            }
        }
    }
}