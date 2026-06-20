using Microsoft.Maui.Graphics;

namespace dinospace
{
    // Central theme palette. Single fixed theme (frosted glass on dark).
    // UI built in code reads these; XAML uses the matching {StaticResource ...Light/...Dark} keys.
    public static class Theme
    {
        public static Color Surface => Color.FromArgb("#E0000000"); // frosted black panel
        public static Color Border => Color.FromArgb("#30FFFFFF"); // subtle white border
        public static Color TextPrimary => Color.FromArgb("#F5F7FB");   // near-white body text
        public static Color TextSecondary => Color.FromArgb("#AEB6C6");   // muted secondary text
        public static Color TextHint => Color.FromArgb("#7E8696");   // placeholder / hint text
        public static Color ChipBg => Color.FromArgb("#33FFFFFF"); // unselected chip background
        public static Color ChipText => Color.FromArgb("#EAEDF3");   // unselected chip text
        public static Color ImgPlaceholder => Color.FromArgb("#26FFFFFF"); // shown before image loads
        public static Color Accent => Color.FromArgb("#E8D5B0");   // brass/cream highlight color
        public static Color Danger => Color.FromArgb("#FF6B6B");   // wrong answer / destructive action
        public static Color QuizCorrect => Color.FromArgb("#5BD17F");   // correct answer highlight
    }

    public static class UiHelpers
    {
        // Standard dino list row: thumbnail + name/group chip + short description
        public static View BuildDinoRow(Dinosaur dino, EventHandler<TappedEventArgs> tapped)
        {
            var image = new Image
            {
                Source = dino.ImageFile,
                WidthRequest = 56,
                HeightRequest = 56,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Theme.ImgPlaceholder
            };

            var name = new Label
            {
                Text = dino.Name,
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                FontFamily = "Baloo",
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center
            };

            // Name + group chip side by side
            var titleRow = new HorizontalStackLayout { Spacing = 8 };
            titleRow.Add(name);
            titleRow.Add(Chip(dino.Group));

            var sub = new Label { Text = dino.ShortDescription, FontSize = 12, TextColor = Theme.TextSecondary };

            var info = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            info.Add(titleRow);
            info.Add(sub);

            return Wrap(image, info, dino, tapped);
        }

        // Standard space object list row: thumbnail + name/type chip + short description
        public static View BuildSpaceRow(SpaceObject obj, EventHandler<TappedEventArgs> tapped)
        {
            var image = new Image
            {
                Source = obj.ImageFile,
                WidthRequest = 56,
                HeightRequest = 56,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Theme.ImgPlaceholder
            };

            var name = new Label
            {
                Text = obj.Name,
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                FontFamily = "Baloo",
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center
            };

            // Name + type chip side by side
            var titleRow = new HorizontalStackLayout { Spacing = 8 };
            titleRow.Add(name);
            titleRow.Add(Chip(obj.TypeLabel));

            var desc = new Label { Text = obj.ShortDescription, FontSize = 12, TextColor = Theme.TextSecondary };

            var info = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            info.Add(titleRow);
            info.Add(desc);

            return Wrap(image, info, obj, tapped);
        }

        // Collection row: name on top, one highlighted stat below (used in ranked lists)
        public static View BuildCollectionRow(string imageFile, string name, string statText, object data, EventHandler<TappedEventArgs> tapped)
        {
            var image = new Image
            {
                Source = imageFile,
                WidthRequest = 56,
                HeightRequest = 56,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Theme.ImgPlaceholder
            };

            var nameLabel = new Label { Text = name, FontSize = 17, FontAttributes = FontAttributes.Bold, FontFamily = "Baloo", TextColor = Theme.TextPrimary };
            var statLabel = new Label { Text = statText, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextPrimary };

            var info = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            info.Add(nameLabel);
            info.Add(statLabel);

            return Wrap(image, info, data, tapped);
        }

        // Compact row used in the live search dropdowns (Home + Saved)
        public static View BuildSearchResultRow(string title, string subtitle, object data, EventHandler<TappedEventArgs> tapped)
        {
            var name = new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold, FontFamily = "Baloo", TextColor = Theme.TextPrimary };
            var sub = new Label { Text = subtitle, FontSize = 12, TextColor = Theme.TextSecondary };

            var info = new VerticalStackLayout { Spacing = 1 };
            info.Add(name);
            info.Add(sub);

            var frame = new Frame
            {
                Padding = new Thickness(14, 10),
                CornerRadius = 0,
                BorderColor = Colors.Transparent,
                BackgroundColor = Theme.Surface,
                HasShadow = false,
                Content = info,
                BindingContext = data
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += tapped;
            frame.GestureRecognizers.Add(tap);
            return frame;
        }

        // Shared card wrapper: thumbnail | info | chevron, inside a frosted Frame
        private static View Wrap(Image image, View info, object data, EventHandler<TappedEventArgs> tapped)
        {
            var chevron = new Label
            {
                Text = "\u203A",
                FontSize = 22,
                TextColor = Theme.TextHint,
                VerticalOptions = LayoutOptions.Center
            };

            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(image, 0, 0);
            grid.Add(info, 1, 0);
            grid.Add(chevron, 2, 0);

            var frame = new Frame
            {
                Padding = new Thickness(12),
                CornerRadius = 14,
                BorderColor = Theme.Border,
                BackgroundColor = Theme.Surface,
                HasShadow = false,
                Margin = new Thickness(0, 4),
                Content = grid,
                BindingContext = data
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += tapped;
            frame.GestureRecognizers.Add(tap);
            return frame;
        }

        // Small rounded label chip (e.g. "Theropod", "Planet")
        public static View Chip(string text)
        {
            return new Frame
            {
                Padding = new Thickness(8, 3),
                CornerRadius = 8,
                BackgroundColor = Theme.ChipBg,
                BorderColor = Colors.Transparent,
                HasShadow = false,
                VerticalOptions = LayoutOptions.Center,
                Content = new Label { Text = text, FontSize = 11, TextColor = Theme.ChipText }
            };
        }
    }
}