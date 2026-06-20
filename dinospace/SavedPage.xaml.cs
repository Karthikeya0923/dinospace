namespace dinospace
{
    public partial class SavedPage : ContentPage
    {
        // Navigation guard to prevent double-taps opening two detail pages
        private bool _isNavigating = false;
        private DateTime _lastNav = DateTime.MinValue;

        // Persists the active filter tab across OnAppearing calls
        private string _currentFilter = "All";

        public SavedPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Release nav guard only after a real return (not a spurious re-appear from pushing a detail page)
            if ((DateTime.Now - _lastNav).TotalMilliseconds > 500)
                _isNavigating = false;

            CloseDropdown();
            SelectFilter(_currentFilter); // rebuild list in case saves changed while away
        }

        // Filter tab handlers
        private void OnFilterAll(object sender, EventArgs e) => SelectFilter("All");
        private void OnFilterDino(object sender, EventArgs e) => SelectFilter("Dinosaurs");
        private void OnFilterSpace(object sender, EventArgs e) => SelectFilter("Space");

        // Apply the chosen filter: style the buttons and rebuild the list
        private void SelectFilter(string filter)
        {
            _currentFilter = filter;
            StyleButton(FilterAll, filter == "All");
            StyleButton(FilterDino, filter == "Dinosaurs");
            StyleButton(FilterSpace, filter == "Space");
            BuildList();
        }

        // Highlight the active tab button in Accent; inactive tabs use ChipBg
        private void StyleButton(Button btn, bool selected)
        {
            btn.BackgroundColor = selected ? Theme.Accent : Theme.ChipBg;
            btn.TextColor = selected ? Colors.Black : Theme.TextPrimary;
        }

        // Rebuild the saved items list for the current filter
        private void BuildList()
        {
            SavedStack.Children.Clear();

            var savedDinos = SavedManager.GetSavedDinos();
            var savedSpace = SavedManager.GetSavedSpace();
            bool hasAny = false;

            if (_currentFilter == "All" || _currentFilter == "Dinosaurs")
            {
                var dinos = DinosaurData.GetAll()
                    .Where(d => savedDinos.Contains(d.Name))
                    .OrderBy(d => d.Name)
                    .ToList();

                foreach (var d in dinos)
                {
                    SavedStack.Children.Add(UiHelpers.BuildDinoRow(d, OnDinoTapped));
                    hasAny = true;
                }
            }

            if (_currentFilter == "All" || _currentFilter == "Space")
            {
                var space = SpaceData.GetAll()
                    .Where(s => savedSpace.Contains(s.Name))
                    .OrderBy(s => s.Name)
                    .ToList();

                foreach (var s in space)
                {
                    SavedStack.Children.Add(UiHelpers.BuildSpaceRow(s, OnSpaceTapped));
                    hasAny = true;
                }
            }

            // Empty state placeholder
            if (!hasAny)
            {
                SavedStack.Children.Add(new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 10,
                    Margin = new Thickness(0, 60),
                    Children =
                    {
                        new Label { Text = "+",                    FontSize = 64, TextColor = Theme.TextHint,      HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "No saved items yet",   FontSize = 16, TextColor = Theme.TextSecondary, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Search above or tap the bookmark on any dinosaur or space object to save it",
                                    FontSize = 12, TextColor = Theme.TextHint, HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center }
                    }
                });
            }
        }

        // ===== Live search dropdown =====

        // Rebuild dropdown as the user types; up to 3 dinos + 3 space objects
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string query = (e.NewTextValue ?? "").Trim().ToLower();
            DropdownStack.Children.Clear();

            if (string.IsNullOrEmpty(query)) { ShowDropdown(false); return; }

            var dinos = DinosaurData.GetAll()
                .Where(d => d.Name.ToLower().Contains(query))
                .OrderBy(d => d.Name).Take(3).ToList();

            var space = SpaceData.GetAll()
                .Where(s => s.Name.ToLower().Contains(query))
                .OrderBy(s => s.Name).Take(3).ToList();

            if (dinos.Count == 0 && space.Count == 0) { ShowDropdown(false); return; }

            foreach (var d in dinos)
                DropdownStack.Children.Add(UiHelpers.BuildSearchResultRow(d.Name, "Dinosaur", d, OnResultSelected));
            foreach (var s in space)
                DropdownStack.Children.Add(UiHelpers.BuildSearchResultRow(s.Name, s.TypeLabel, s, OnResultSelected));

            ShowDropdown(true);
        }

        private void ShowDropdown(bool show)
        {
            SearchDropdown.IsVisible = show;
            DismissLayer.IsVisible = show;
        }

        // Clear search field and hide the dropdown
        private void CloseDropdown()
        {
            SearchEntry.Text = "";
            DropdownStack.Children.Clear();
            ShowDropdown(false);
        }

        private void OnDismissTapped(object sender, EventArgs e)
        {
            CloseDropdown();
            SearchEntry.Unfocus();
        }

        // Tapping a dropdown result immediately saves that entry
        private void OnResultSelected(object sender, EventArgs e)
        {
            if (sender is View v)
            {
                if (v.BindingContext is Dinosaur d)
                    SavedManager.SaveDino(d.Name);
                else if (v.BindingContext is SpaceObject s)
                    SavedManager.SaveSpace(s.Name);

                CloseDropdown();
                SearchEntry.Unfocus();
                SelectFilter(_currentFilter);
            }
        }

        // Save button saves the first dino or space object matching the typed query
        private void OnSaveClicked(object sender, EventArgs e)
        {
            string query = SearchEntry.Text?.Trim().ToLower() ?? "";
            if (string.IsNullOrEmpty(query)) return;

            var matchedDino = DinosaurData.GetAll().FirstOrDefault(d => d.Name.ToLower().Contains(query));
            var matchedSpace = SpaceData.GetAll().FirstOrDefault(s => s.Name.ToLower().Contains(query));

            if (matchedDino != null)
                SavedManager.SaveDino(matchedDino.Name);
            else if (matchedSpace != null)
                SavedManager.SaveSpace(matchedSpace.Name);

            CloseDropdown();
            SearchEntry.Unfocus();
            SelectFilter(_currentFilter);
        }

        // ===== Saved item taps =====

        private async void OnDinoTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            if (sender is View v && v.BindingContext is Dinosaur d)
            {
                _isNavigating = true;
                _lastNav = DateTime.Now;
                await Shell.Current.Navigation.PushAsync(new DinoDetailPage(d), false);
            }
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