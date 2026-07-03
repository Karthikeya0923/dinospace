using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Credits, privacy summary, and version info.
    public class AboutPage : ContentPage
    {
        public AboutPage()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16, 4, 16, 28) };

            stack.Add(new Label { Text = "About DinoSpace", FontFamily = Ui.Display, FontSize = Ui.S(26), TextColor = Theme.TextPrimary });

            stack.Add(DetailUi.Section("The app",
                "DinoSpace brings the worlds of dinosaurs and space together in one friendly place to explore. Browse a rich encyclopedia, test yourself with quizzes, stage dino battles, and ask NovaSaur — an AI that runs entirely on your device — anything you're curious about.",
                Theme.AccentNova));

            stack.Add(DetailUi.Section("NovaSaur AI",
                "NovaSaur is powered by an on-device language model grounded in DinoSpace's own encyclopedia, so answers are safe, kid-friendly, and work with no internet connection. Everything you ask stays on your phone.",
                Theme.AccentNova));

            stack.Add(DetailUi.Section("Privacy",
                "DinoSpace does not collect personal information. Your progress, bookmarks, and chats are stored only on your device. NovaSaur runs offline, so your questions are never sent anywhere.",
                Theme.AccentSpace));

            stack.Add(DetailUi.Section("Credits",
                "Made with curiosity for young explorers everywhere. Dinosaur and space facts are drawn from widely accepted science, simplified for a general audience.",
                Theme.AccentDino));

            stack.Add(new Label { Text = "Version 2.0", FontFamily = Ui.Fonts, FontSize = Ui.S(13), TextColor = Theme.TextHint, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 10) });

            var content = Nav.DetailScaffold("About", new ScrollView { Content = stack }, Theme.AccentNova, out _);
            Content = new Grid { BackgroundColor = Theme.Bg, Children = { content } };
            SwipeBack.Attach(this);
        }
    }
}
