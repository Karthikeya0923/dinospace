using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace dinospace.Services
{
    // User preferences: haptics and text size. Kept tiny and global so any
    // screen can read them without plumbing.
    public static class AppSettings
    {
        // 0 = Off, 1 = Light, 2 = Medium, 3 = Strong. Older installs migrate
        // from the on/off switch: on -> Medium, off -> Off.
        public static int HapticLevel
        {
            get => Preferences.Get("set_hapticlevel", Preferences.Get("set_haptics", true) ? 2 : 0);
            set => Preferences.Set("set_hapticlevel", value);
        }

        public static bool Haptics => HapticLevel > 0;

        public static bool DarkMode
        {
            get => Preferences.Get("set_darkmode", false);
            set => Preferences.Set("set_darkmode", value);
        }

        // Theme id ("theme1".."theme7"); old installs may still say "classic",
        // which Theme.ApplyCurrent maps onto Fossil / Dark Mode.
        public static string ThemeId
        {
            get => Preferences.Get("set_theme", "theme1");
            set => Preferences.Set("set_theme", value);
        }

        // Layout id: "native" (the editorial default) or "playful" (the big,
        // rounded, kid-first layout). Drives fonts, shapes, the tab bar and
        // the home screen through AppLayout.
        public static string LayoutId
        {
            get => Preferences.Get("set_layout", "native");
            set => Preferences.Set("set_layout", value);
        }

        // 0 = Small, 1 = Default, 2 = Large, 3 = Extra large.
        public static int TextSizeIndex
        {
            get => Preferences.Get("set_textsize", 1);
            set => Preferences.Set("set_textsize", value);
        }

        public static double FontScale => TextSizeIndex switch
        {
            0 => 0.9,
            2 => 1.15,
            3 => 1.3,
            _ => 1.0,
        };

        // A clear, feelable tap, scaled by the chosen strength. A vibration
        // pulse is far more noticeable than the system "click" haptic, which
        // many phones barely render.
        public static void Tap()
        {
            int ms = HapticLevel switch { 1 => 10, 2 => 18, 3 => 30, _ => 0 };
            if (ms == 0) return;
            try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms)); }
            catch { try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { } }
        }

        // A stronger pulse for saves and important confirmations.
        public static void LongPress()
        {
            int ms = HapticLevel switch { 1 => 20, 2 => 35, 3 => 55, _ => 0 };
            if (ms == 0) return;
            try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms)); }
            catch { try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { } }
        }
    }
}
