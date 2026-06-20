namespace dinospace
{
    public partial class CollectionPage : ContentPage
    {
        // Guard against double-taps triggering multiple navigations
        private bool _isNavigating = false;
        private DateTime _lastNav = DateTime.MinValue;

        public CollectionPage(string collectionId)
        {
            InitializeComponent();
            Build(collectionId);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Release nav guard only after a real return (not a spurious re-appear from pushing a detail page)
            if ((DateTime.Now - _lastNav).TotalMilliseconds > 500)
                _isNavigating = false;
        }

        // Set title/subtitle and populate the ranked list based on collection id
        private void Build(string id)
        {
            if (id == "predators")
            {
                TitleLabel.Text = "Apex Predators";
                SubtitleLabel.Text = "Ranked by raw power, strongest first";
                var list = DinosaurData.GetAll()
                    .Where(d => IsFilledDino(d) && d.Strength > 0)
                    .OrderByDescending(d => d.Strength)
                    .ToList();
                AddDinos(list, d => $"Power: {d.Strength}");
            }
            else if (id == "speed")
            {
                TitleLabel.Text = "Speed Demons";
                SubtitleLabel.Text = "Ranked by top speed, fastest first";
                var list = DinosaurData.GetAll()
                    .Where(IsFilledDino)
                    .OrderByDescending(d => ParseNumber(d.Speed))
                    .ToList();
                AddDinos(list, d => d.Speed);
            }
            else if (id == "giants")
            {
                TitleLabel.Text = "Colossal Giants";
                SubtitleLabel.Text = "Ranked by weight, heaviest first";
                var list = DinosaurData.GetAll()
                    .Where(IsFilledDino)
                    .OrderByDescending(d => ParseNumber(d.Weight))
                    .ToList();
                AddDinos(list, d => d.Weight);
            }
            else if (id == "space_distance")
            {
                TitleLabel.Text = "A Journey From Earth";
                SubtitleLabel.Text = "Space objects from nearest to farthest";
                // Exclude Earth itself; only include objects with a parseable distance
                var list = SpaceData.GetAll()
                    .Where(s => IsFilledSpace(s) && s.Name != "Earth" && ParseKm(FindStat(s, "Distance")) > 0)
                    .OrderBy(s => ParseKm(FindStat(s, "Distance")))
                    .ToList();
                AddSpace(list, s => FindStat(s, "Distance"));
            }
            else if (id == "space_size")
            {
                TitleLabel.Text = "Cosmic Giants";
                SubtitleLabel.Text = "The largest objects in space, biggest first";
                var list = SpaceData.GetAll()
                    .Where(s => IsFilledSpace(s) && ParseKm(FindStat(s, "Diameter")) > 0)
                    .OrderByDescending(s => ParseKm(FindStat(s, "Diameter")))
                    .ToList();
                AddSpace(list, s => FindStat(s, "Diameter"));
            }
        }

        // Add ranked dinosaur rows to the list
        private void AddDinos(List<Dinosaur> dinos, Func<Dinosaur, string> stat)
        {
            int rank = 1;
            foreach (var d in dinos)
            {
                var row = UiHelpers.BuildCollectionRow(d.ImageFile, d.Name, stat(d), d, OnDinoTapped);
                ListStack.Children.Add(RankWrap(rank, row));
                rank++;
            }
        }

        // Add ranked space object rows to the list
        private void AddSpace(List<SpaceObject> space, Func<SpaceObject, string> stat)
        {
            int rank = 1;
            foreach (var s in space)
            {
                var row = UiHelpers.BuildCollectionRow(s.ImageFile, s.Name, stat(s), s, OnSpaceTapped);
                ListStack.Children.Add(RankWrap(rank, row));
                rank++;
            }
        }

        // Wrap a row with a rank number; gold/silver/bronze for top 3
        private View RankWrap(int rank, View row)
        {
            Color rankColor;
            if (rank == 1) rankColor = Color.FromArgb("#D4AF37"); // gold
            else if (rank == 2) rankColor = Color.FromArgb("#9E9E9E"); // silver
            else if (rank == 3) rankColor = Color.FromArgb("#CD7F32"); // bronze
            else rankColor = Theme.TextSecondary;       // muted for the rest

            var num = new Label
            {
                Text = rank.ToString(),
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = rankColor,
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 26,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var grid = new Grid { ColumnSpacing = 4 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.Add(num, 0, 0);
            grid.Add(row, 1, 0);
            return grid;
        }

        private async void OnBackTapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PopAsync();
        }

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

        // Only include entries that have real content (not placeholder text)
        private bool IsFilledDino(Dinosaur d) => !string.IsNullOrEmpty(d.AboutText) && !d.AboutText.StartsWith("Change");
        private bool IsFilledSpace(SpaceObject s) => !string.IsNullOrEmpty(s.AboutText) && !s.AboutText.StartsWith("Change");

        // Extract the leading numeric value from a string like "8,000 kg" or "27 km/h"
        private double ParseNumber(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var cleaned = s.Replace(",", "");
            var sb = new System.Text.StringBuilder();
            foreach (char c in cleaned)
            {
                if (char.IsDigit(c) || c == '.') sb.Append(c);
                else if (sb.Length > 0) break;
            }
            return double.TryParse(sb.ToString(), out var v) ? v : 0;
        }

        // Convert a distance/size string to kilometres for consistent sorting
        private double ParseKm(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            string lower = s.ToLower();
            double mult = 1;
            if (lower.Contains("light-year") || lower.Contains("light year")) mult *= 9.4607e12;
            if (lower.Contains("billion")) mult *= 1e9;
            else if (lower.Contains("million")) mult *= 1e6;
            else if (lower.Contains("thousand")) mult *= 1e3;
            return ParseNumber(s) * mult;
        }

        // Find a stat value on a SpaceObject by partial label match (e.g. "Distance", "Diameter")
        private string FindStat(SpaceObject o, string labelContains)
        {
            var pairs = new (string label, string val)[]
            {
                (o.Stat1Label, o.Stat1Value),
                (o.Stat2Label, o.Stat2Value),
                (o.Stat3Label, o.Stat3Value),
                (o.Stat4Label, o.Stat4Value),
            };
            foreach (var p in pairs)
                if (!string.IsNullOrEmpty(p.label) && p.label.ToLower().Contains(labelContains.ToLower()))
                    return p.val;
            return "";
        }
    }
}