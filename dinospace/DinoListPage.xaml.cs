namespace dinospace
{
    // Dead code — superseded by DinoPediaPage with search and filters.
    // Kept in the project to avoid breaking any lingering references.
    public partial class DinoListPage : ContentPage
    {
        private bool _isNavigating = false;

        public DinoListPage()
        {
            InitializeComponent();
            BuildList();
        }

        // Build a simple scrollable list of every dinosaur
        private void BuildList()
        {
            var dinosaurs = DinosaurData.GetAll();

            foreach (var dino in dinosaurs)
            {
                var frame = new Frame
                {
                    Padding = new Thickness(16, 14),
                    CornerRadius = 12,
                    BorderColor = Colors.LightGray,
                    BackgroundColor = Colors.White
                };

                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    }
                };

                grid.Add(new Label
                {
                    Text = dino.Name,
                    FontSize = 17,
                    VerticalOptions = LayoutOptions.Center
                }, 0);

                grid.Add(new Label
                {
                    Text = "›",
                    FontSize = 20,
                    TextColor = Colors.LightGray,
                    VerticalOptions = LayoutOptions.Center
                }, 1);

                frame.Content = grid;

                // Capture dino in a local variable so the lambda closes over the right instance
                var captured = dino;
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    if (_isNavigating) return;
                    _isNavigating = true;
                    await Shell.Current.Navigation.PushAsync(new DinoDetailPage(captured));
                    _isNavigating = false;
                };

                frame.GestureRecognizers.Add(tapGesture);
                DinoStack.Children.Add(frame);
            }
        }
    }
}