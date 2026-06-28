using Microsoft.Maui.Graphics;

namespace dinospace
{
    public partial class CollectionPage : ContentPage
    {
        private bool _isNavigating = false;
        private DateTime _lastNav = DateTime.MinValue;

        private readonly string _collectionId;
        private bool _listBuilt = false;

        public CollectionPage(string collectionId)
        {
            InitializeComponent();
            _collectionId = collectionId;
            SwipeBack.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if ((DateTime.Now - _lastNav).TotalMilliseconds > 500)
                _isNavigating = false;

            if (!_listBuilt)
            {
                _listBuilt = true;
                Dispatcher.Dispatch(async () =>
                {
                    await Task.Delay(1);
                    Build(_collectionId);
                });
            }
        }

        private void Build(string id)
        {
            var items = new List<RankedItem>();

            if (id == "predators")
            {
                TitleLabel.Text = "Apex Predators";
                SubtitleLabel.Text = "Ranked by raw power, strongest first";
                var list = DinosaurData.GetAll().Where(d => IsFilledDino(d) && d.Strength > 0).OrderByDescending(d => d.Strength).ToList();
                AddDinos(items, list, d => $"Power: {d.Strength}");
            }
            else if (id == "speed")
            {
                TitleLabel.Text = "Speed Demons";
                SubtitleLabel.Text = "Ranked by top speed, fastest first";
                var list = DinosaurData.GetAll().Where(IsFilledDino).OrderByDescending(d => ParseNumber(d.Speed)).ToList();
                AddDinos(items, list, d => d.Speed);
            }
            else if (id == "giants")
            {
                TitleLabel.Text = "Colossal Giants";
                SubtitleLabel.Text = "Ranked by weight, heaviest first";
                var list = DinosaurData.GetAll().Where(IsFilledDino).OrderByDescending(d => ParseNumber(d.Weight)).ToList();
                AddDinos(items, list, d => d.Weight);
            }
            else if (id == "space_distance")
            {
                TitleLabel.Text = "A Journey From Earth";
                SubtitleLabel.Text = "Space objects from nearest to farthest";
                var list = SpaceData.GetAll().Where(s => IsFilledSpace(s) && s.Name != "Earth" && ParseKm(FindStat(s, "Distance")) > 0).OrderBy(s => ParseKm(FindStat(s, "Distance"))).ToList();
                AddSpace(items, list, s => FindStat(s, "Distance"));
            }
            else if (id == "space_size")
            {
                TitleLabel.Text = "Cosmic Giants";
                SubtitleLabel.Text = "The largest objects in space, biggest first";
                var list = SpaceData.GetAll().Where(s => IsFilledSpace(s) && ParseKm(FindStat(s, "Diameter")) > 0).OrderByDescending(s => ParseKm(FindStat(s, "Diameter"))).ToList();
                AddSpace(items, list, s => FindStat(s, "Diameter"));
            }

            RankList.ItemsSource = items;
        }

        private void AddDinos(List<RankedItem> items, List<Dinosaur> dinos, Func<Dinosaur, string> stat)
        {
            int rank = 1;
            foreach (var d in dinos)
            {
                items.Add(new RankedItem { Rank = rank, RankColor = RankColor(rank), ImageFile = d.ImageFile, Name = d.Name, Stat = stat(d), Data = d });
                rank++;
            }
        }

        private void AddSpace(List<RankedItem> items, List<SpaceObject> space, Func<SpaceObject, string> stat)
        {
            int rank = 1;
            foreach (var s in space)
            {
                items.Add(new RankedItem { Rank = rank, RankColor = RankColor(rank), ImageFile = s.ImageFile, Name = s.Name, Stat = stat(s), Data = s });
                rank++;
            }
        }

        private Color RankColor(int rank)
        {
            if (rank == 1) return Color.FromArgb("#D4AF37");
            if (rank == 2) return Color.FromArgb("#9E9E9E");
            if (rank == 3) return Color.FromArgb("#CD7F32");
            return Theme.TextSecondary;
        }

        private async void OnBackTapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PopAsync();
        }

        // Tap a ranked row -> open the dino or space object it carries.
        private async void OnRowTapped(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            if (sender is View v && v.BindingContext is RankedItem item)
            {
                _isNavigating = true;
                _lastNav = DateTime.Now;
                if (item.Data is Dinosaur d)
                    await Shell.Current.Navigation.PushAsync(new DinoDetailPage(d));
                else if (item.Data is SpaceObject s)
                    await Shell.Current.Navigation.PushAsync(new SpaceDetailPage(s));
            }
        }

        private bool IsFilledDino(Dinosaur d) => !string.IsNullOrEmpty(d.AboutText) && !d.AboutText.StartsWith("Change");
        private bool IsFilledSpace(SpaceObject s) => !string.IsNullOrEmpty(s.AboutText) && !s.AboutText.StartsWith("Change");

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

    // One ranked row's data for the CollectionView.
    public class RankedItem
    {
        public int Rank { get; set; }
        public string RankText => Rank.ToString();
        public Color RankColor { get; set; } = Colors.Gray;
        public string ImageFile { get; set; } = "";
        public string Name { get; set; } = "";
        public string Stat { get; set; } = "";
        public object Data { get; set; }
    }
}