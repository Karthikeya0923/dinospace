using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace dinospace.Views
{
    // Editor for one of the user's own lists: rename it, write a subtitle,
    // add any mix of dinosaurs and space objects, remove entries, or delete
    // the whole thing. Everything autosaves.
    public class CustomListPage : ContentPage
    {
        private CustomList _list;
        private VerticalStackLayout _entriesArea = null!;

        public CustomListPage(CustomList list)
        {
            _list = list;
            Build();
            SwipeBack.Attach(this);
        }

        private void Build()
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(18, 4, 18, 28) };

            // Editable title + subtitle, styled like the rest of the app.
            var title = new Entry
            {
                Text = _list.Title,
                Placeholder = "Name your list",
                FontFamily = Ui.Display, FontSize = Ui.S(32),
                TextColor = Theme.TextPrimary, PlaceholderColor = Theme.TextHint,
                BackgroundColor = Colors.Transparent
            };
            title.TextChanged += (_, e) => { _list.Title = (e.NewTextValue ?? "").Trim(); CustomListStore.Update(_list); };
            stack.Add(title);

            var subtitle = new Entry
            {
                Text = _list.Subtitle,
                Placeholder = "Add a subtitle (like \"my dream zoo\")",
                FontFamily = Ui.Fonts, FontSize = Ui.S(14),
                TextColor = Theme.TextSecondary, PlaceholderColor = Theme.TextHint,
                BackgroundColor = Colors.Transparent
            };
            subtitle.TextChanged += (_, e) => { _list.Subtitle = (e.NewTextValue ?? "").Trim(); CustomListStore.Update(_list); };
            stack.Add(subtitle);

            stack.Add(Ui.SectionHeader("Entries"));
            _entriesArea = new VerticalStackLayout { Spacing = 10 };
            stack.Add(_entriesArea);
            RefreshEntries();

            stack.Add(Ui.PrimaryButton("Add an entry", async (_, _) =>
                await Nav.Push(() => new EntryPickerPage(OnPicked), animated: false)));

            var delete = new Label
            {
                Text = "Delete this list",
                FontFamily = Ui.Display, FontSize = Ui.S(17), TextColor = Theme.Danger,
                HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 14, 0, 0)
            };
            Ui.OnTap(delete, async (_, _) => await ConfirmDelete());
            stack.Add(delete);

            var body = Nav.DetailScaffoldFixed("", new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never });
            Content = Ui.PageRoot(body);
        }

        private void RefreshEntries()
        {
            _entriesArea.Children.Clear();

            if (_list.Entries.Count == 0)
            {
                _entriesArea.Add(Ui.Card(Ui.Muted("Nothing here yet — tap “Add an entry” and build your dream line-up. Dinosaurs and space stuff can mix!"), 16, new Thickness(16, 14)));
                return;
            }

            foreach (var key in _list.Entries.ToList())
            {
                var resolved = CustomListStore.Resolve(key);
                if (resolved == null) continue;
                var (image, name, meta, data) = resolved.Value;

                var thumbGrid = new Grid { BackgroundColor = Color.FromArgb("#111527") };
                thumbGrid.Add(new Label
                {
                    Text = name[..1].ToUpperInvariant(),
                    FontFamily = Ui.Display, FontSize = 18, TextColor = Color.FromArgb("#E3BE55"),
                    HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
                });
                // User drawings show whole on their canvas colour; built-in art fills.
                string drawingBg = data is Dinosaur dd ? dd.CreationBg : data is SpaceObject ss ? ss.CreationBg : "";
                if (drawingBg.Length == 0)
                    thumbGrid.Add(new Image { Source = image, Aspect = Aspect.AspectFill, WidthRequest = 46, HeightRequest = 46 });
                else
                    thumbGrid.Add(EntryCards.Drawing(image, drawingBg));
                var thumb = new Border
                {
                    Content = thumbGrid, WidthRequest = 46, HeightRequest = 46,
                    Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    VerticalOptions = LayoutOptions.Center
                };

                var info = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
                info.Add(new Label { Text = name, FontFamily = Ui.Display, FontSize = Ui.S(16.5), TextColor = Theme.TextPrimary });
                info.Add(new Label { Text = meta, FontFamily = Ui.Fonts, FontSize = Ui.S(11.5), TextColor = Theme.TextSecondary });

                var remove = Ui.Icon(Ui.IconClose, 20);
                var removeWrap = new Border
                {
                    Content = remove, WidthRequest = 38, HeightRequest = 38,
                    BackgroundColor = Colors.Transparent, Stroke = Colors.Transparent,
                    VerticalOptions = LayoutOptions.Center
                };
                string k = key;
                Ui.OnTap(removeWrap, (_, _) =>
                {
                    _list.Entries.Remove(k);
                    CustomListStore.Update(_list);
                    RefreshEntries();
                });
                Ui.Describe(removeWrap, $"Remove {name} from this list");

                var row = new Grid { ColumnSpacing = 12 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Add(thumb, 0, 0);
                row.Add(info, 1, 0);
                row.Add(removeWrap, 2, 0);

                var card = Ui.Card(row, 14, new Thickness(12, 10));
                Ui.OnTap(card, async (_, _) =>
                {
                    if (k.StartsWith("c:")) await Nav.Push(() => new CreationDetailPage(k[2..]));
                    else if (data is Dinosaur d) await Nav.OpenDino(d);
                    else if (data is SpaceObject s) await Nav.OpenSpace(s);
                });
                _entriesArea.Add(card);
            }
        }

        private void OnPicked(object data)
        {
            string key = CustomListStore.KeyFor(data);
            if (key.Length == 0 || _list.Entries.Contains(key)) return;
            _list.Entries.Add(key);
            CustomListStore.Update(_list);
            RefreshEntries();
        }

        private async System.Threading.Tasks.Task ConfirmDelete()
        {
            bool sure = await DisplayAlertAsync("Delete this list?",
                $"“{(_list.Title.Length > 0 ? _list.Title : "Untitled list")}” and its {_list.Entries.Count} entries will be gone. The encyclopedia itself isn't touched.",
                "Delete", "Keep it");
            if (!sure) return;
            CustomListStore.Delete(_list.Id);
            try { await Shell.Current.Navigation.PopAsync(); } catch { }
        }
    }
}
