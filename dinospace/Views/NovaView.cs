using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace dinospace.Views
{
    // NovaSaur chat. Owns three states: the one-time model setup/download
    // overlay, the "waking up" model init, and the live conversation with a
    // typewriter reveal. Kept as a persistent tab so the chat is preserved.
    public class NovaView : ContentView, ITabView
    {
        private const string HistoryKey = "nova_history_v2";
        private const int MaxSaved = 60;
        private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(90);

        private const string Welcome =
            "Hey, I'm NovaSaur! Ask me anything about dinosaurs or space — like how big a T. Rex was, or why Mars is red. I run right here on your device.";

        // A detail page can queue a question and switch to this tab.
        private static string? _pending;
        public static void Ask(string question) => _pending = question;

        private List<ChatMessage> _messages = new();
        private List<string> _lastEntities = new();

        private VerticalStackLayout _chatStack = null!;
        private ScrollView _chatScroll = null!;
        private Entry _entry = null!;
        private Border _sendBtn = null!;
        private Label _sendIcon = null!;
        private HorizontalStackLayout _suggestions = null!;
        private ScrollView _suggestionScroll = null!;
        private Grid _inputArea = null!;

        // download overlay
        private Grid _overlay = null!;
        private Label _overlayTitle = null!, _overlayBody = null!, _overlayStatus = null!;
        private Border _downloadBtn = null!;
        private Label _downloadBtnLabel = null!;
        private ProgressBar _progress = null!;
        private VerticalStackLayout _progressArea = null!;
        private HorizontalStackLayout _pauseRow = null!;
        private Label _pauseLabel = null!;
        private IDispatcherTimer? _packTimer;
        private bool _subscribed;

        // answering
        private bool _busy;
        private int _gen;
        private bool _chatStarted;
        private bool _modelInited;

        // reveal
        private Label? _revealLabel;
        private string[]? _revealWords;
        private int _revealIndex;
        private bool _revealActive;
        private readonly StringBuilder _revealSb = new();
        private IDispatcherTimer? _revealTimer;
        private View? _thinking;

        public NovaView() => Build();

        public void OnSelected()
        {
            if (ModelManager.IsModelDownloaded())
            {
                StartChat();
            }
            else
            {
                ModelManager.TryBeginBundledInstall();
                ShowOverlay();
                Subscribe();
                RefreshOverlay();
                StartPackRecheck();
            }
        }

        // ---------- layout ----------
        private void Build()
        {
            _chatStack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(16, 12, 16, 8) };
            _chatScroll = new ScrollView { Content = _chatStack, VerticalScrollBarVisibility = ScrollBarVisibility.Never };

            // header
            var header = BuildHeader();

            // suggestions
            _suggestions = new HorizontalStackLayout { Spacing = 8 };
            _suggestionScroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _suggestions, Padding = new Thickness(16, 4) };

            // input
            _inputArea = BuildInput();

            var main = new Grid { RowSpacing = 0 };
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.Add(header, 0, 0);
            main.Add(_chatScroll, 0, 1);
            main.Add(_suggestionScroll, 0, 2);
            main.Add(_inputArea, 0, 3);

            _overlay = BuildOverlay();

            var root = new Grid();
            root.Add(main);
            root.Add(_overlay);
            Content = root;
        }

        private View BuildHeader()
        {
            var dot = new Border
            {
                WidthRequest = 40, HeightRequest = 40,
                BackgroundColor = Ui.MultiplyAlpha(Theme.AccentNova, 0.18f),
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Content = new Label { Text = "✦", FontSize = 20, TextColor = Theme.AccentNova, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
            };
            var title = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
            title.Add(new Label { Text = "NovaSaur", FontFamily = Ui.Display, FontSize = 19, TextColor = Theme.TextPrimary });
            title.Add(new Label { Text = "Offline dino & space guide", FontFamily = Ui.Fonts, FontSize = 11.5, TextColor = Theme.TextSecondary });

            var clear = new Label { Text = "Clear", FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center };
            Ui.OnTap(clear, (_, _) => ClearChat());

            var grid = new Grid { Padding = new Thickness(16, 14, 16, 8), ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(dot, 0, 0); grid.Add(title, 1, 0); grid.Add(clear, 2, 0);
            return grid;
        }

        private Grid BuildInput()
        {
            _entry = new Entry { Placeholder = "Ask about dinosaurs or space...", BackgroundColor = Colors.Transparent, ReturnType = ReturnType.Send };
            _entry.Completed += (_, _) => OnSend();
            var entryWrap = new Border
            {
                Content = _entry,
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                Padding = new Thickness(16, 0)
            };

            _sendIcon = new Label { Text = "➤", FontSize = 18, TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            _sendBtn = new Border
            {
                Content = _sendIcon,
                WidthRequest = 46, HeightRequest = 46,
                BackgroundColor = Theme.AccentNova,
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 23 }
            };
            Ui.OnTap(_sendBtn, (_, _) => OnSend());
            Ui.Describe(_sendBtn, "Send question");

            var grid = new Grid { Padding = new Thickness(16, 8, 16, 14), ColumnSpacing = 10 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Add(entryWrap, 0, 0);
            grid.Add(_sendBtn, 1, 0);
            return grid;
        }

        // ---------- chat ----------
        private void StartChat()
        {
            if (_chatStarted) { MaybeRunPending(); return; }
            _chatStarted = true;
            StopPackRecheck();
            _overlay.IsVisible = false;
            LoadHistory();
            InitModel();
        }

        private async void InitModel()
        {
            if (!NovaSaurService.SupportedPlatform)
            {
                if (_messages.Count == 0) AddNova("NovaSaur runs on Android right now. The rest of DinoSpace works everywhere!");
                _sendBtn.IsEnabled = false;
                _suggestionScroll.IsVisible = false;
                return;
            }

            if (!_modelInited && !NovaSaurService.IsReady)
            {
                var status = AddStatus("NovaSaur is waking up… first time takes a moment.");
                try { await NovaSaurService.InitAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nova init: " + ex); }
                Remove(status);
            }
            _modelInited = true;

            if (!NovaSaurService.IsReady)
            {
                AddNova("I couldn't start up — your device may be low on free memory. Try closing other apps and reopening this tab.");
                _sendBtn.IsEnabled = false;
                _suggestionScroll.IsVisible = false;
                return;
            }

            if (_messages.Count == 0) AddNova(Welcome);
            ShowSuggestions();
            MaybeRunPending();
        }

        private void MaybeRunPending()
        {
            if (_pending == null || _busy || !NovaSaurService.IsReady) return;
            string q = _pending; _pending = null;
            AddUser(q);
            Answer(q);
        }

        private void OnSend()
        {
            if (_busy) { StopGeneration(); return; }
            string q = (_entry.Text ?? "").Trim();
            if (q.Length == 0) return;
            _entry.Text = "";
            AddUser(q);
            Answer(q);
        }

        private async void Answer(string question)
        {
            _busy = true;
            _sendIcon.Text = "■";
            int myGen = ++_gen;
            _thinking = AddStatus("NovaSaur is thinking…");
            await ScrollToEnd();

            NovaTurn turn;
            try { turn = PromptBuilder.Build(question, _messages, _lastEntities); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nova prompt: " + ex); FinishThinking(myGen, NovaSaurService.ErrorMessage); return; }

            if (turn.Entities.Count > 0) _lastEntities = new List<string>(turn.Entities);

            if (turn.InstantReply != null)
            {
                await Task.Delay(300);
                FinishThinking(myGen, turn.InstantReply);
                return;
            }

            string answer;
            try { answer = await NovaSaurService.AskAsync(turn.Prompt!, AnswerTimeout, CancellationToken.None); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nova ask: " + ex); answer = NovaSaurService.ErrorMessage; }

            if (myGen != _gen) return;
            FinishThinking(myGen, answer);
        }

        private void StopGeneration()
        {
            if (_revealActive && _revealLabel != null) { FinishRevealNow(); return; }
            _gen++;
            if (_thinking != null) { Remove(_thinking); _thinking = null; }
            _busy = false;
            _sendIcon.Text = "➤";
        }

        private void FinishThinking(int myGen, string answer)
        {
            if (myGen != _gen) return;
            if (_thinking != null) { Remove(_thinking); _thinking = null; }
            Reveal(answer);
        }

        // ---------- typewriter reveal ----------
        private void Reveal(string text)
        {
            _revealWords = text.Split(' ');
            _revealIndex = 0;
            _revealSb.Clear();
            _revealLabel = StartNovaBubble();
            _revealActive = true;

            _revealTimer ??= MakeTimer(TimeSpan.FromMilliseconds(40), RevealTick);
            _revealTimer.Start();
        }

        private void RevealTick()
        {
            if (_revealWords == null || _revealIndex >= _revealWords.Length) { CompleteReveal(); return; }
            _revealSb.Append(_revealWords[_revealIndex]);
            if (_revealIndex < _revealWords.Length - 1) _revealSb.Append(' ');
            _revealIndex++;
            if (_revealLabel != null) _revealLabel.Text = _revealSb.ToString();
            _ = _chatScroll.ScrollToAsync(0, _chatStack.Height, false);
        }

        private void FinishRevealNow()
        {
            if (_revealWords != null && _revealLabel != null)
            {
                _revealSb.Clear();
                _revealSb.Append(string.Join(" ", _revealWords));
                _revealLabel.Text = _revealSb.ToString();
            }
            CompleteReveal();
        }

        private void CompleteReveal()
        {
            _revealTimer?.Stop();
            if (!_revealActive) return;
            _revealActive = false;
            string full = _revealSb.ToString().Trim();
            if (full.Length > 0) { _messages.Add(new ChatMessage { IsUser = false, Text = full }); SaveHistory(); }
            _revealLabel = null; _revealWords = null;
            _busy = false; _sendIcon.Text = "➤";
            ShowSuggestions();
            _ = ScrollToEnd();
        }

        // ---------- bubbles ----------
        private void AddUser(string text) => AddMessage(text, true);
        private void AddNova(string text) => AddMessage(text, false);

        private void AddMessage(string text, bool isUser)
        {
            _messages.Add(new ChatMessage { IsUser = isUser, Text = text });
            _chatStack.Add(Bubble(text, isUser));
            SaveHistory();
            _ = ScrollToEnd();
        }

        private View Bubble(string text, bool isUser)
        {
            var label = new Label { Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(15), LineHeight = 1.4, TextColor = isUser ? Theme.TextOnAccent : Theme.TextPrimary };
            var bubble = new Border
            {
                Content = label,
                Padding = new Thickness(14, 11),
                BackgroundColor = isUser ? Theme.AccentNova : Theme.Surface,
                Stroke = isUser ? Colors.Transparent : Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = isUser ? new CornerRadius(16, 16, 16, 4) : new CornerRadius(16, 16, 4, 16) },
                HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
                MaximumWidthRequest = 300
            };
            if (!isUser) Ui.OnTap(bubble, async (_, _) => await AnswerMenu(label.Text), haptic: false);
            return bubble;
        }

        private Label StartNovaBubble()
        {
            var label = new Label { Text = "", FontFamily = Ui.Fonts, FontSize = Ui.S(15), LineHeight = 1.4, TextColor = Theme.TextPrimary };
            var bubble = new Border
            {
                Content = label,
                Padding = new Thickness(14, 11),
                BackgroundColor = Theme.Surface,
                Stroke = Theme.HairlineSoft, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16, 16, 4, 16) },
                HorizontalOptions = LayoutOptions.Start,
                MaximumWidthRequest = 300
            };
            Ui.OnTap(bubble, async (_, _) => await AnswerMenu(label.Text), haptic: false);
            _chatStack.Add(bubble);
            return label;
        }

        private View AddStatus(string text)
        {
            var label = new Label { Text = text, FontFamily = Ui.Fonts, FontSize = Ui.S(13), FontAttributes = FontAttributes.Italic, TextColor = Theme.TextSecondary, HorizontalOptions = LayoutOptions.Start };
            _chatStack.Add(label);
            _ = ScrollToEnd();
            return label;
        }

        private void Remove(View v) { if (v != null && _chatStack.Contains(v)) _chatStack.Remove(v); }

        private async Task AnswerMenu(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            string choice = await page.DisplayActionSheet(null, "Cancel", null, "Copy", "Share");
            if (choice == "Copy") { try { await Clipboard.Default.SetTextAsync(text); } catch { } }
            else if (choice == "Share") await DetailUi.ShareText(text);
        }

        // ---------- suggestions ----------
        private void ShowSuggestions()
        {
            _suggestions.Children.Clear();
            foreach (var q in SuggestedQuestions.Pick(4))
            {
                var label = new Label { Text = q, FontFamily = Ui.Fonts, FontSize = 12.5, TextColor = Theme.ChipText };
                var chip = new Border
                {
                    Content = label,
                    BackgroundColor = Theme.ChipBg, Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 16 },
                    Padding = new Thickness(14, 8)
                };
                Ui.OnTap(chip, (_, _) => { if (_busy) return; _entry.Text = q; OnSend(); });
                _suggestions.Add(chip);
            }
            _suggestionScroll.IsVisible = true;
        }

        private void ClearChat()
        {
            _gen++;
            _revealTimer?.Stop();
            _revealActive = false; _revealLabel = null; _revealWords = null;
            if (_thinking != null) { Remove(_thinking); _thinking = null; }
            _busy = false; _sendIcon.Text = "➤";
            _messages.Clear();
            _lastEntities.Clear();
            Preferences.Remove(HistoryKey);
            _chatStack.Children.Clear();
            AddNova(Welcome);
            ShowSuggestions();
        }

        // ---------- history ----------
        private void SaveHistory()
        {
            try
            {
                if (_messages.Count > MaxSaved) _messages.RemoveRange(0, _messages.Count - MaxSaved);
                Preferences.Set(HistoryKey, JsonSerializer.Serialize(_messages));
            }
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
                foreach (var m in _messages) _chatStack.Add(Bubble(m.Text, m.IsUser));
            }
            catch { }
        }

        private async Task ScrollToEnd()
        {
            await Task.Delay(40);
            await _chatScroll.ScrollToAsync(0, _chatStack.Height, true);
        }

        private IDispatcherTimer MakeTimer(TimeSpan interval, Action tick)
        {
            var t = Dispatcher.CreateTimer();
            t.Interval = interval;
            t.Tick += (_, _) => tick();
            return t;
        }

        // ---------- download overlay ----------
        private Grid BuildOverlay()
        {
            _overlayTitle = new Label { Text = "Meet NovaSaur", FontFamily = Ui.Display, FontSize = 26, TextColor = Theme.TextPrimary, HorizontalTextAlignment = TextAlignment.Center };
            _overlayBody = new Label { FontFamily = Ui.Fonts, FontSize = 14.5, LineHeight = 1.45, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center };
            _overlayStatus = new Label { FontFamily = Ui.Fonts, FontSize = 13, TextColor = Theme.TextSecondary, HorizontalTextAlignment = TextAlignment.Center };

            _progress = new ProgressBar { Progress = 0, HeightRequest = 8 };
            _pauseLabel = new Label { Text = "Pause", FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.AccentNova };
            Ui.OnTap(_pauseLabel, (_, _) => OnPauseResume());
            var stopLabel = new Label { Text = "Stop", FontFamily = Ui.Fonts, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Theme.Danger };
            Ui.OnTap(stopLabel, (_, _) => OnStop());
            _pauseRow = new HorizontalStackLayout { Spacing = 24, HorizontalOptions = LayoutOptions.Center, Children = { _pauseLabel, stopLabel } };

            _progressArea = new VerticalStackLayout { Spacing = 12, IsVisible = false };
            _progressArea.Add(_overlayStatus);
            _progressArea.Add(_progress);
            _progressArea.Add(_pauseRow);

            _downloadBtnLabel = new Label { Text = "Download NovaSaur", FontFamily = Ui.Fonts, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextOnAccent, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            _downloadBtn = new Border
            {
                Content = _downloadBtnLabel,
                BackgroundColor = Theme.AccentNova, Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(20, 14)
            };
            Ui.OnTap(_downloadBtn, (_, _) => OnDownload());

            var icon = new Border
            {
                WidthRequest = 72, HeightRequest = 72,
                BackgroundColor = Ui.MultiplyAlpha(Theme.AccentNova, 0.18f),
                Stroke = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 36 },
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label { Text = "✦", FontSize = 34, TextColor = Theme.AccentNova, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
            };

            var card = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(24), VerticalOptions = LayoutOptions.Center };
            card.Add(icon);
            card.Add(_overlayTitle);
            card.Add(_overlayBody);
            card.Add(_downloadBtn);
            card.Add(_progressArea);

            var g = new Grid { BackgroundColor = Theme.Bg, Padding = new Thickness(20) };
            g.Add(new ScrollView { Content = card });
            g.IsVisible = false;
            return g;
        }

        private void ShowOverlay() => _overlay.IsVisible = true;

        private void Subscribe()
        {
            if (_subscribed) return;
            ModelManager.Changed += OnModelChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            ModelManager.Changed -= OnModelChanged;
            _subscribed = false;
        }

        private void OnModelChanged() => MainThread.BeginInvokeOnMainThread(RefreshOverlay);

        private void StartPackRecheck()
        {
            _packTimer ??= MakeTimer(TimeSpan.FromSeconds(5), () =>
            {
                if (_chatStarted || ModelManager.State == DownloadState.Downloading) return;
                if (ModelManager.TryBeginBundledInstall()) RefreshOverlay();
            });
            _packTimer.Start();
        }

        private void StopPackRecheck() => _packTimer?.Stop();

        private void RefreshOverlay()
        {
            switch (ModelManager.State)
            {
                case DownloadState.Completed:
                    Unsubscribe(); StopPackRecheck(); StartChat();
                    break;
                case DownloadState.Downloading:
                    _downloadBtn.IsVisible = false;
                    _progressArea.IsVisible = true;
                    _progress.Progress = ModelManager.Progress;
                    if (ModelManager.IsLocalInstall)
                    {
                        _overlayTitle.Text = "Getting NovaSaur ready";
                        _overlayBody.Text = "One-time setup, about a minute. You can leave — it finishes in the background.";
                        _overlayStatus.Text = ProgressText("Setting up");
                        _pauseRow.IsVisible = false;
                    }
                    else
                    {
                        _overlayTitle.Text = "Downloading NovaSaur";
                        _overlayBody.Text = "Keep using the app — the download continues in the background, even if you close DinoSpace.";
                        _overlayStatus.Text = ProgressText("Downloading");
                        _pauseRow.IsVisible = true;
                        _pauseLabel.Text = "Pause";
                    }
                    break;
                case DownloadState.Paused:
                    _downloadBtn.IsVisible = false;
                    _progressArea.IsVisible = true;
                    _progress.Progress = ModelManager.Progress;
                    _overlayStatus.Text = ProgressText("Paused at");
                    _pauseLabel.Text = "Resume";
                    break;
                case DownloadState.Failed:
                    _overlayTitle.Text = "Download paused";
                    _overlayBody.Text = "The download stopped. Tap resume and it picks up where it left off.";
                    _downloadBtn.IsVisible = true;
                    _downloadBtnLabel.Text = "Resume download";
                    _progressArea.IsVisible = false;
                    break;
                default:
                    _overlayTitle.Text = "Meet NovaSaur";
                    if (ModelManager.BundledPartsFound() > 0)
                    {
                        _overlayBody.Text = "Google Play is finishing NovaSaur's setup in the background. It'll appear here automatically in a few minutes.";
                        _downloadBtn.IsVisible = false;
                    }
                    else
                    {
                        _overlayBody.Text = "An offline AI that answers your dinosaur and space questions, right on your phone. It's a one-time download of about 3 GB (wifi recommended).";
                        _downloadBtn.IsVisible = true;
                        _downloadBtnLabel.Text = ModelManager.HasPartialDownload() ? "Resume download" : "Download NovaSaur";
                    }
                    _progressArea.IsVisible = false;
                    break;
            }
        }

        private string ProgressText(string prefix)
        {
            int pct = (int)(ModelManager.Progress * 100);
            long total = ModelManager.TotalBytes;
            if (total > 0)
            {
                double done = ModelManager.DoneBytes / 1_000_000_000.0;
                double tot = total / 1_000_000_000.0;
                return $"{prefix} {pct}% ({done:0.0} of {tot:0.0} GB)";
            }
            return $"{prefix} {pct}%";
        }

        private async void OnDownload()
        {
            if (ModelManager.State == DownloadState.Downloading) return;
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            long free = ModelManager.GetFreeSpaceBytes();
            long needed = Math.Max(500_000_000, ModelManager.RequiredFreeBytes - ModelManager.GetPartialSizeBytes());
            if (free >= 0 && free < needed && page != null)
            {
                await page.DisplayAlert("Not enough space", $"NovaSaur needs about {needed / 1_000_000_000.0:0.0} GB free. Free up some space and try again.", "OK");
                return;
            }
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet && page != null)
            {
                await page.DisplayAlert("No internet", "Connect to the internet to download NovaSaur.", "OK");
                return;
            }
            if (!Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.WiFi) && page != null)
            {
                bool go = await page.DisplayAlert("Not on wifi", "This is a large download (about 3 GB). Downloading over mobile data may use up your plan. Download anyway?", "Download", "Wait for wifi");
                if (!go) return;
            }
            try { await Permissions.RequestAsync<Permissions.PostNotifications>(); } catch { }
            ModelManager.Start();
            RefreshOverlay();
        }

        private void OnPauseResume()
        {
            if (ModelManager.State == DownloadState.Downloading) ModelManager.Pause();
            else ModelManager.Start();
            RefreshOverlay();
        }

        private async void OnStop()
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            bool sure = await page.DisplayAlert("Stop download?", "This deletes what's downloaded so far. You'd start over next time.", "Stop", "Keep downloading");
            if (!sure) return;
            ModelManager.Stop();
            RefreshOverlay();
        }
    }
}
