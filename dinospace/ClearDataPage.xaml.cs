namespace dinospace
{
    public partial class ClearDataPage : ContentPage
    {
        // Represents one toggleable reset option in the list
        private class ResetItem
        {
            public string Key;       // identifier used in ResetCategory()
            public bool Checked;     // current toggle state
            public Frame Box;        // the checkbox frame
            public Label Tick;       // the checkmark label inside the box
        }

        private readonly List<ResetItem> _items = new();

        public ClearDataPage()
        {
            InitializeComponent();
            SwipeBack.Attach(this);
            BuildItems();
        }

        // Populate the list with all resettable data categories
        private void BuildItems()
        {
            AddItem("saved", "Saved items", "Your bookmarked dinosaurs and space objects");
            AddItem("quiz", "Quiz scores", "Your overall quiz accuracy");
            AddItem("streak", "Daily streak", "Your day-streak counter");
            AddItem("views", "Most viewed", "Which entries you've opened most");
        }

        // Build one row: a checkbox + title/subtitle, wired to a ResetItem
        private void AddItem(string key, string title, string subtitle)
        {
            // Checkmark tick (hidden until selected)
            var tick = new Label
            {
                Text = "✓",
                TextColor = Colors.White,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false,
                InputTransparent = true
            };

            // Checkbox box that holds the tick
            var box = new Frame
            {
                WidthRequest = 26,
                HeightRequest = 26,
                CornerRadius = 6,
                Padding = 0,
                HasShadow = false,
                BorderColor = Theme.Border,
                BackgroundColor = Theme.Surface,
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true,
                Content = tick
            };

            // Title + subtitle text
            var name = new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold };
            var sub = new Label { Text = subtitle, FontSize = 12, TextColor = Theme.TextSecondary };
            var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            textStack.Add(name);
            textStack.Add(sub);

            // Checkbox on the left, text on the right
            var grid = new Grid { ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.Add(box, 0, 0);
            grid.Add(textStack, 1, 0);

            // Outer card frame
            var frame = new Frame
            {
                Padding = new Thickness(14, 12),
                CornerRadius = 14,
                BorderColor = Theme.Border,
                BackgroundColor = Theme.Surface,
                HasShadow = false,
                Content = grid
            };

            var item = new ResetItem { Key = key, Box = box, Tick = tick, Checked = false };

            // Toggle this item when the row is tapped
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => OnItemTapped(item);
            frame.GestureRecognizers.Add(tap);

            _items.Add(item);
            ItemsStack.Children.Add(frame);
        }

        // Toggle a single item and refresh the Select All label
        private void OnItemTapped(ResetItem item)
        {
            SetChecked(item, !item.Checked);
            UpdateSelectAllLabel();
        }

        // If all are checked, uncheck all; otherwise check all
        private void OnSelectAllTapped(object sender, EventArgs e)
        {
            bool selectAll = !_items.All(i => i.Checked);
            foreach (var it in _items) SetChecked(it, selectAll);
            UpdateSelectAllLabel();
        }

        // Apply checked/unchecked visual state to a single item
        private void SetChecked(ResetItem item, bool value)
        {
            item.Checked = value;
            if (value)
            {
                item.Box.BackgroundColor = Theme.Accent;
                item.Box.BorderColor = Theme.Accent;
                item.Tick.IsVisible = true;
            }
            else
            {
                item.Box.BackgroundColor = Theme.Surface;
                item.Box.BorderColor = Theme.Border;
                item.Tick.IsVisible = false;
            }
        }

        // Switch label between "Select All" and "Clear Selection"
        private void UpdateSelectAllLabel()
        {
            SelectAllLabel.Text = _items.All(i => i.Checked) ? "Clear Selection" : "Select All";
        }

        // Confirm then wipe all checked categories
        private async void OnClearSelectedClicked(object sender, EventArgs e)
        {
            var selected = _items.Where(i => i.Checked).ToList();
            if (selected.Count == 0)
            {
                await DisplayAlertAsync("Nothing Selected", "Pick at least one thing to clear first.", "OK");
                return;
            }

            bool confirm = await DisplayAlertAsync(
                "Clear Data",
                "This will permanently clear the selected data. Are you sure?",
                "Clear",
                "Cancel");

            if (!confirm) return;

            foreach (var it in selected)
                ResetCategory(it.Key);

            // Uncheck everything after clearing
            foreach (var it in _items) SetChecked(it, false);
            UpdateSelectAllLabel();

            await DisplayAlertAsync("Done", "The selected data has been cleared.", "OK");
        }

        // Wipe stored data for the given category key
        private void ResetCategory(string key)
        {
            switch (key)
            {
                case "saved":
                    // Remove all saved dinos and space objects
                    foreach (var n in SavedManager.GetSavedDinos().ToList()) SavedManager.UnsaveDino(n);
                    foreach (var n in SavedManager.GetSavedSpace().ToList()) SavedManager.UnsaveSpace(n);
                    break;

                case "quiz":
                    // Clear correct/total counts for each quiz mode
                    Preferences.Remove("quiz_correct_Dinosaurs");
                    Preferences.Remove("quiz_questions_Dinosaurs");
                    Preferences.Remove("quiz_correct_Space");
                    Preferences.Remove("quiz_questions_Space");
                    Preferences.Remove("quiz_correct_Mixed");
                    Preferences.Remove("quiz_questions_Mixed");
                    break;

                case "streak":
                    // Reset streak count and last-active date
                    Preferences.Remove("streak_count");
                    Preferences.Remove("streak_last_date");
                    break;

                case "views":
                    // Clear view-count preference for every dino and space object
                    foreach (var d in DinosaurData.GetAll()) Preferences.Remove($"views_{d.Name}");
                    foreach (var s in SpaceData.GetAll()) Preferences.Remove($"views_{s.Name}");
                    break;
            }
        }

        private async void OnBackTapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PopAsync();
        }
    }
}