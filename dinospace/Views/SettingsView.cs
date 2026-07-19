using System;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Settings, straight from the design sheet: a profile card up top, then
    // one rounded group of icon rows — appearance, sound, novasaur ai,
    // privacy, about — each with a little line icon and a chevron.
    public class SettingsView : ContentView, ITabView
    {
        public SettingsView() => Build();

        public void OnSelected() { }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(18, 8, 18, 28) };

            stack.Add(ProfileCard());

            stack.Add(Group(
                IconRow(Ui.IconAppearance, "Appearance", async () => await Nav.Push(() => new ThemesPage())),
                SoundRow(),
                // Parent mode: open directly for setup; once it's on, the
                // controls only open through the PIN pad.
                IconRow(Ui.IconSettings, "Parent mode", async () =>
                {
                    if (!ParentMode.Enabled)
                        await Nav.Push(() => new ParentModePage());
                    else
                        await Nav.Push(() => new ParentPinPage(ParentPinPage.PinMode.Unlock,
                            async () => await Nav.Push(() => new ParentModePage())));
                }),
                IconRow(Ui.IconNovaAi, "Nova AI", async () => await Nav.Push(() => new HostPage("nova ai", NovaAiBody()))),
                IconRow(Ui.IconPrivacy, "Privacy", OpenPrivacy),
                IconRow(Ui.IconAbout, "About DinoSpace", ShowAbout),
                IconRow(Ui.IconContact, "Contact us", OpenFeedback)));

            stack.Add(Group(DangerRow("Reset progress & bookmarks", ConfirmReset)));

            stack.Add(new Label
            {
                Text = $"dinospace v{AppInfo.Current.VersionString}",
                FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextHint,
                HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, 16, 0, 0)
            });

            Content = new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        // Your card: the profile picture (mascot_pfp.png once it's drawn,
        // an empty circle until then) beside "You". Tapping it opens the
        // profile page with everything the app knows about your journey.
        private View ProfileCard()
        {
            // The pfp art is itself a round badge (cropped tight to its own
            // edge), so it renders at the frame's full size and BECOMES the
            // circle — sized any smaller it floats as a circle-in-a-circle.
            var face = new Border
            {
                WidthRequest = 54, HeightRequest = 54,
                BackgroundColor = Theme.AccentSoft,
                Stroke = Theme.Hairline.WithAlpha(0.5f), StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 27 },
                Content = Ui.Mascot("mascot_pfp", 54),
                VerticalOptions = LayoutOptions.Center
            };

            var who = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
            who.Add(new Label { Text = "You", FontFamily = Ui.Display, FontSize = Ui.S(18), TextColor = Theme.TextPrimary });
            who.Add(new Label { Text = "Explorer", FontFamily = Ui.Fonts, FontSize = Ui.S(12.5), TextColor = Theme.TextSecondary });

            var chev = Ui.Icon(Ui.IconChevron, 20);
            chev.VerticalOptions = LayoutOptions.Center;

            var grid = new Grid { ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(face, 0, 0);
            grid.Add(who, 1, 0);
            grid.Add(chev, 2, 0);

            var card = new Border
            {
                Content = grid, BackgroundColor = Theme.Surface,
                Stroke = Theme.CardStroke, StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(16, 14), Margin = new Thickness(0, 0, 0, 4),
                Shadow = Theme.CardShadow()
            };
            Ui.OnTap(card, async (_, _) => await Nav.Push(() => new ProfilePage()));
            Ui.Describe(card, "Open your profile");
            return card;
        }

        // One rounded card holding a run of rows with faint dividers.
        private static View Group(params View[] rows)
        {
            var col = new VerticalStackLayout { Spacing = 0 };
            for (int i = 0; i < rows.Length; i++)
            {
                col.Add(rows[i]);
                if (i < rows.Length - 1) col.Add(new BoxView { HeightRequest = 1, Color = Theme.HairlineSoft });
            }
            return new Border
            {
                Content = col, BackgroundColor = Theme.Surface,
                Stroke = Theme.CardStroke, StrokeThickness = 1.4,
                StrokeShape = new RoundRectangle { CornerRadius = 20 }, Padding = new Thickness(16, 2),
                Margin = new Thickness(0, 4), Shadow = Theme.CardShadow()
            };
        }

        // icon · title · chevron, like every row on the sheet.
        private View IconRow(string icon, string title, Action onTap)
        {
            var grid = RowShell(icon, title);
            var chev = Ui.Icon(Ui.IconChevron, 20);
            chev.VerticalOptions = LayoutOptions.Center;
            grid.Add(chev, 2, 0);
            Ui.OnTap(grid, (_, _) => onTap());
            return grid;
        }

        // icon · title · toggle. Haptics on/off (medium strength when on).
        private View SoundRow()
        {
            var sw = new Switch
            {
                IsToggled = AppSettings.Haptics,
                OnColor = Theme.Accent, ThumbColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            };
            sw.Toggled += (_, e) =>
            {
                AppSettings.HapticLevel = e.Value ? 2 : 0;
                if (e.Value) AppSettings.Tap();
            };
            var grid = RowShell(Ui.IconSound, "Sound & haptics");
            grid.Add(sw, 2, 0);
            return grid;
        }

        private Grid RowShell(string icon, string title)
        {
            var ic = Ui.Icon(icon, 22);
            ic.VerticalOptions = LayoutOptions.Center;

            var grid = new Grid { Padding = new Thickness(0, 15), ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(ic, 0, 0);
            grid.Add(new Label
            {
                Text = title, FontFamily = Ui.Display, FontSize = Ui.S(16.5),
                TextColor = Theme.TextPrimary, VerticalOptions = LayoutOptions.Center
            }, 1, 0);
            return grid;
        }

        private View DangerRow(string title, Action onTap)
        {
            var grid = new Grid { Padding = new Thickness(0, 15) };
            grid.Add(new Label
            {
                Text = title,
                FontFamily = Ui.Display, FontSize = Ui.S(16.5),
                TextColor = Theme.Danger, VerticalOptions = LayoutOptions.Center
            });
            Ui.OnTap(grid, (_, _) => onTap());
            return grid;
        }

        // The NovaSaur model manager, hosted on its own pushed page.
        private static View NovaAiBody()
        {
            var col = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(18, 4, 18, 28) };
            col.Add(new Label
            {
                Text = "Nova answers instantly from its encyclopedia. Add the optional on-device AI model and open-ended questions get full streamed answers — still completely offline.",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary
            });
            col.Add(new NovaModelCard(hideWhenInstalled: false));
            return new ScrollView { Content = col };
        }

        private async void OpenPrivacy()
        {
            try { await Launcher.OpenAsync("https://github.com/Karthikeya0923/dinospace/blob/main/PRIVACY_POLICY.md"); } catch { }
        }

        private async void ShowAbout()
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            await page.DisplayAlertAsync($"DinoSpace v{AppInfo.Current.VersionString}",
                "A fully offline dinosaur & space encyclopedia with its own on-device AI and a live sky scanner.\n\nA few of the creatures inside — like Megalodon, Titanoboa and the woolly mammoth — aren't true dinosaurs, just amazing prehistoric creatures who earned their page.",
                "OK");
        }

        private async void OpenFeedback()
        {
            try { await Launcher.OpenAsync("mailto:dinospace.app@gmail.com?subject=DinoSpace%20Feedback"); } catch { }
        }

        private async void ConfirmReset()
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            bool sure = await page.DisplayAlertAsync("Reset everything?",
                "This clears your streak, quiz scores, viewed history, bookmarks, and your Nova chat. The encyclopedia and the AI itself stay. This can't be undone.",
                "Reset", "Cancel");
            if (!sure) return;
            StatsStore.ClearProgress();
            SavedStore.ClearAll();
            NovaView.DeleteSavedChat();
        }
    }
}
