using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // The parent mode screen. Before parent mode is on it explains what it
    // does and offers to set it up; once on (reached through the PIN pad) it
    // holds the two feature switches and the way to turn it all off again.
    public class ParentModePage : ContentPage
    {
        private readonly ContentView _body = new();

        public ParentModePage()
        {
            Shell.SetNavBarIsVisible(this, false);
            Content = Ui.PageRoot(Nav.DetailScaffoldFixed("parent mode", _body));
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            _body.Content = ParentMode.Enabled ? Controls() : Intro();
        }

        // ---------- before setup ----------

        private View Intro()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(24, 10, 24, 30) };

            stack.Add(Ui.Body(
                "Parent mode lets a grown-up decide which parts of DinoSpace are open. " +
                "It can switch off Ask Nova and Scan Sky — the encyclopedia, quizzes, " +
                "battles and drawing always stay on."));
            stack.Add(Ui.Muted(
                "You'll choose a 4-digit PIN. From then on, this screen only opens with " +
                "that PIN, and turning parent mode off brings the app back to normal."));

            var go = Ui.PrimaryButton("turn on parent mode", async (_, _) =>
                await Nav.Push(() => new ParentPinPage(ParentPinPage.PinMode.Set, Build)));
            go.Margin = new Thickness(0, 12, 0, 0);
            stack.Add(go);

            return new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        // ---------- once on ----------

        private View Controls()
        {
            var stack = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(24, 6, 24, 30) };

            stack.Add(Ui.SectionHeader("What's allowed"));
            stack.Add(ToggleRow("Ask Nova", "The chat that answers dinosaur and space questions",
                ParentMode.NovaAllowed, v => ParentMode.SetAllowNova(v)));
            stack.Add(Hairline());
            stack.Add(ToggleRow("Scan Sky", "The live camera view of tonight's stars",
                ParentMode.SkyAllowed, v => ParentMode.SetAllowSky(v)));

            stack.Add(Ui.Muted("Anything switched off shows a small note instead of opening, until a grown-up allows it again.", 12.5));

            var off = Ui.GhostButton("turn off parent mode", async (_, _) =>
            {
                bool sure = false;
                try { sure = await DisplayAlertAsync(Ui.T("Turn off parent mode?"), Ui.T("The PIN is removed and everything is allowed again."), Ui.T("Turn off"), Ui.T("Cancel")); }
                catch { }
                if (!sure) return;
                ParentMode.Disable();
                AppSettings.Tap();
                try
                {
                    var nav = Shell.Current?.Navigation;
                    if (nav != null && nav.NavigationStack.Count > 1) await nav.PopAsync();
                }
                catch { }
            });
            off.Margin = new Thickness(0, 26, 0, 0);
            stack.Add(off);

            return new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };
        }

        private static View ToggleRow(string title, string blurb, bool state, Action<bool> onChange)
        {
            var sw = new Switch
            {
                IsToggled = state,
                OnColor = Theme.Accent, ThumbColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            };
            sw.Toggled += (_, e) => { onChange(e.Value); AppSettings.Tap(); };

            var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label { Text = Ui.T(title), FontFamily = Ui.Display, FontSize = Ui.S(16.5), TextColor = Theme.TextPrimary });
            info.Add(new Label { Text = Ui.T(blurb), FontFamily = Ui.Fonts, FontSize = Ui.S(12), TextColor = Theme.TextSecondary });

            var grid = new Grid { Padding = new Thickness(0, 14), ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(info, 0, 0);
            grid.Add(sw, 1, 0);
            return grid;
        }

        private static BoxView Hairline() => new() { HeightRequest = 1, Color = Theme.HairlineSoft };
    }
}
