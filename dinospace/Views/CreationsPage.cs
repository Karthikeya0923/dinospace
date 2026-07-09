using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using dinospace.Models;
using dinospace.Services;

namespace dinospace.Views
{
    // "Your Creations" — the gallery of everything the user has drawn and
    // described. Rebuilds each time it appears so a freshly saved creation
    // shows up immediately.
    public class CreationsPage : ContentPage
    {
        private VerticalStackLayout _stack = null!;

        public CreationsPage()
        {
            Build();
            SwipeBack.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Refresh();
        }

        private void Build()
        {
            _stack = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(18, 4, 18, 28) };
            var body = Nav.DetailScaffoldFixed("", new ScrollView { Content = _stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never });
            Content = Ui.PageRoot(body);
        }

        private void Refresh()
        {
            _stack.Children.Clear();
            _stack.Add(new Label { Text = "your creations", FontFamily = Ui.Display, FontSize = Ui.S(30), TextColor = Theme.TextPrimary });
            _stack.Add(new Label
            {
                Text = "Draw your own creatures and space objects, give them stats, and battle the ones you make.",
                FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), LineHeight = 1.4, TextColor = Theme.TextSecondary
            });

            _stack.Add(Ui.PrimaryButton("Create something new", async (_, _) => await Nav.Push(() => new CreationEditorPage())));

            // the cheering mascot's spot (mascot_draw.png), with its little
            // "can't wait to see your discovery!" bubble
            if (Ui.HasImage("mascot_draw"))
            {
                var bubble = new Border
                {
                    Content = new Label { Text = "can't wait to see your discovery!", FontFamily = Ui.Fonts, FontSize = Ui.S(13.5), TextColor = Theme.TextPrimary },
                    BackgroundColor = Theme.Surface,
                    Stroke = Theme.CardStroke, StrokeThickness = 1.2,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
                    Padding = new Thickness(14, 10), VerticalOptions = LayoutOptions.Center
                };
                var row = new Grid { ColumnSpacing = 10, Margin = new Thickness(0, 2, 0, 0) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                row.Add(Ui.Mascot("mascot_draw", 84), 0, 0);
                row.Add(bubble, 1, 0);
                _stack.Add(row);
            }

            var all = CreationStore.All();
            if (all.Count == 0)
            {
                // Copy kept short enough to wrap the same way at the card's
                // measure and layout widths — text near that boundary gets its
                // last line clipped on Android.
                _stack.Add(Ui.Card(Ui.Muted("Nothing here yet! Tap the button above and draw your very first creature or planet."), 16, new Thickness(16, 16)));
                return;
            }

            var dinos = all.Where(c => c.Kind == CreationKind.Dinosaur).ToList();
            var spaces = all.Where(c => c.Kind == CreationKind.Space).ToList();

            if (dinos.Count > 0)
            {
                _stack.Add(Ui.SectionHeader($"Your dinosaurs ({dinos.Count})"));
                _stack.Add(Grid2(dinos));
            }
            if (spaces.Count > 0)
            {
                _stack.Add(Ui.SectionHeader($"Your space objects ({spaces.Count})"));
                _stack.Add(Grid2(spaces));
            }
        }

        // Two-column grid of creation cards.
        private View Grid2(List<UserCreation> items)
        {
            var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            int rows = (items.Count + 1) / 2;
            for (int r = 0; r < rows; r++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int i = 0; i < items.Count; i++)
            {
                var card = Card(items[i]);
                grid.Add(card, i % 2, i / 2);
            }
            return grid;
        }

        private View Card(UserCreation c)
        {
            var imgGrid = new Grid { HeightRequest = 130, BackgroundColor = Colors.White };
            imgGrid.Add(EntryCards.ArtFallback(c.Name, 30, stars: false));
            if (!string.IsNullOrEmpty(c.ImagePath) && System.IO.File.Exists(c.ImagePath))
                imgGrid.Add(EntryCards.Drawing(c.ImagePath, c.CanvasColor));

            var imgWrap = new Border
            {
                Content = imgGrid, HeightRequest = 130,
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 14 }
            };

            var name = new Label { Text = c.Name, FontFamily = Ui.Display, FontSize = Ui.S(16), TextColor = Theme.TextPrimary, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };
            var meta = new Label { Text = c.MetaLine, FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), TextColor = Theme.TextSecondary, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };

            var col = new VerticalStackLayout { Spacing = 5, Children = { imgWrap, name, meta } };
            var card = new Border
            {
                Content = col,
                BackgroundColor = Theme.Surface, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = new Thickness(8, 8, 8, 12),
                Shadow = Theme.CardShadow()
            };
            Ui.OnTap(card, async (_, _) => await Nav.Push(() => new CreationDetailPage(c.Id)));
            Ui.Describe(card, $"{c.Name}, {c.MetaLine}");
            return card;
        }
    }
}
