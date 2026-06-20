using System.Text.Json;
using Microsoft.Maui.Storage;

namespace dinospace
{
    public partial class AskAiPage : ContentPage
    {
        private const string HistoryKey = "nova_chat_history";
        private bool _busy = false;
        private bool _initStarted = false;
        private List<ChatMessage> _messages = new List<ChatMessage>();

        public AskAiPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_initStarted) return;
            _initStarted = true;
            LoadHistory();
            InitModel();
        }

        private async void InitModel()
        {
#if ANDROID
            try
            {
                if (!Com.Novasaur.NovaSaurModule.IsReady)
                {
                    SendButton.IsEnabled = false;
                    var status = AddStatus("NovaSaur is waking up... (first time takes a moment)");
                    var ctx = Android.App.Application.Context;
                    await Task.Run(() => Com.Novasaur.NovaSaurModule.Init(ctx));
                    RemoveBubble(status);
                }
                SendButton.IsEnabled = true;
                if (_messages.Count == 0)
                    AddNovaBubble("Hi, I'm NovaSaur. Ask me anything about dinosaurs or space.");
            }
            catch (Exception ex)
            {
                AddNovaBubble("Sorry, I couldn't wake up. " + ex.Message);
                SendButton.IsEnabled = false;
            }
#else
            if (_messages.Count == 0)
                AddNovaBubble("NovaSaur runs on Android only right now.");
            SendButton.IsEnabled = false;
#endif
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            if (_busy) return;
            string question = (QuestionEntry.Text ?? "").Trim();
            if (string.IsNullOrEmpty(question)) return;

            _busy = true;
            SendButton.IsEnabled = false;

            AddUserBubble(question);
            QuestionEntry.Text = "";
            await ScrollToBottom();

            var thinking = AddStatus("NovaSaur is thinking...");
            await ScrollToBottom();

#if ANDROID
            try
            {
                string raw = await Task.Run(() => Com.Novasaur.NovaSaurModule.Ask(RagService.BuildPrompt(question)));
                string answer = RagService.CleanAnswer(raw);
                RemoveBubble(thinking);
                AddNovaBubble(string.IsNullOrWhiteSpace(answer)
                    ? "Hmm, I didn't catch that. Try asking another way."
                    : answer);
            }
            catch (Exception ex)
            {
                RemoveBubble(thinking);
                AddNovaBubble("Error: " + ex.Message);
            }
#else
            RemoveBubble(thinking);
#endif
            await ScrollToBottom();
            _busy = false;
            SendButton.IsEnabled = true;
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            _messages.Clear();
            Preferences.Remove(HistoryKey);
            ChatStack.Children.Clear();
            AddNovaBubble("Hi, I'm NovaSaur. Ask me anything about dinosaurs or space.");
        }

        private void AddUserBubble(string text) => AddMessage(text, true);
        private void AddNovaBubble(string text) => AddMessage(text, false);

        private void AddMessage(string text, bool isUser)
        {
            _messages.Add(new ChatMessage { IsUser = isUser, Text = text });
            ChatStack.Children.Add(BuildBubble(text, isUser));
            SaveHistory();
        }

        private void SaveHistory()
        {
            try { Preferences.Set(HistoryKey, JsonSerializer.Serialize(_messages)); }
            catch { }
        }

        private void LoadHistory()
        {
            try
            {
                var json = Preferences.Get(HistoryKey, "");
                if (string.IsNullOrEmpty(json)) return;
                var saved = JsonSerializer.Deserialize<List<ChatMessage>>(json);
                if (saved == null) return;
                _messages = saved;
                foreach (var m in _messages)
                    ChatStack.Children.Add(BuildBubble(m.Text, m.IsUser));
            }
            catch { }
        }

        private View BuildBubble(string text, bool isUser)
        {
            var label = new Label
            {
                Text = text,
                TextColor = isUser ? Colors.White : Theme.TextPrimary,
                FontSize = 15,
                LineHeight = 1.35
            };

            return new Frame
            {
                Content = label,
                Padding = new Thickness(12, 10),
                CornerRadius = 16,
                HasShadow = false,
                BackgroundColor = isUser ? Color.FromArgb("#512BD4") : Theme.Surface,
                BorderColor = isUser ? Color.FromArgb("#512BD4") : Theme.Border,
                HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
                MaximumWidthRequest = 320
            };
        }

        private View AddStatus(string text)
        {
            var label = new Label
            {
                Text = text,
                TextColor = Theme.TextSecondary,
                FontSize = 13,
                FontAttributes = FontAttributes.Italic,
                HorizontalOptions = LayoutOptions.Start
            };
            ChatStack.Children.Add(label);
            return label;
        }

        private void RemoveBubble(View v)
        {
            if (v != null && ChatStack.Children.Contains(v))
                ChatStack.Children.Remove(v);
        }

        private async Task ScrollToBottom()
        {
            await Task.Delay(50);
            await ChatScroll.ScrollToAsync(0, ChatStack.Height, true);
        }
    }

    public class ChatMessage
    {
        public bool IsUser { get; set; }
        public string Text { get; set; } = "";
    }
}