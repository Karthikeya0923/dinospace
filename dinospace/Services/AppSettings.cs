using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace dinospace.Services
{
    // User preferences: haptics and text size. Kept tiny and global so any
    // screen can read them without plumbing.
    public static class AppSettings
    {
        public static bool Haptics
        {
            get => Preferences.Get("set_haptics", true);
            set => Preferences.Set("set_haptics", value);
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

        public static bool Onboarded
        {
            get => Preferences.Get("onboarded_v2", false);
            set => Preferences.Set("onboarded_v2", value);
        }

        // A subtle tap, gated by the user's preference.
        public static void Tap()
        {
            if (!Haptics) return;
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        }

        public static void LongPress()
        {
            if (!Haptics) return;
            try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
        }
    }
}
