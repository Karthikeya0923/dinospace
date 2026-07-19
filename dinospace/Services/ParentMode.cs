using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace dinospace
{
    // Parent mode: a 4-digit PIN a grown-up sets in Settings. While it's on,
    // the two features some families prefer to keep closed — the Nova chat
    // and the live camera sky scanner — can each be switched off, and the
    // settings page that controls them only opens after the PIN.
    //
    // The PIN itself never touches disk: only a salted SHA-256 hash is
    // stored, so nothing readable ever leaves the phone (nothing here is
    // sent anywhere, same as the rest of the app).
    public static class ParentMode
    {
        private const string KeyOn = "parent.on";
        private const string KeyHash = "parent.hash";
        private const string KeySalt = "parent.salt";
        private const string KeyNova = "parent.allowNova";
        private const string KeySky = "parent.allowSky";

        public static bool Enabled => Preferences.Get(KeyOn, false);
        public static bool NovaAllowed => !Enabled || Preferences.Get(KeyNova, true);
        public static bool SkyAllowed => !Enabled || Preferences.Get(KeySky, true);

        public static void SetAllowNova(bool allowed) => Preferences.Set(KeyNova, allowed);
        public static void SetAllowSky(bool allowed) => Preferences.Set(KeySky, allowed);

        public static void Enable(string pin)
        {
            string salt = Guid.NewGuid().ToString("N");
            Preferences.Set(KeySalt, salt);
            Preferences.Set(KeyHash, Hash(pin, salt));
            Preferences.Set(KeyNova, true);
            Preferences.Set(KeySky, true);
            Preferences.Set(KeyOn, true);
        }

        // Turning parent mode off forgets the PIN and reopens everything —
        // the app returns to exactly its default state.
        public static void Disable()
        {
            Preferences.Remove(KeyOn);
            Preferences.Remove(KeyHash);
            Preferences.Remove(KeySalt);
            Preferences.Remove(KeyNova);
            Preferences.Remove(KeySky);
        }

        public static bool Check(string pin)
            => Preferences.Get(KeyHash, "").Length > 0
               && Preferences.Get(KeyHash, "") == Hash(pin, Preferences.Get(KeySalt, ""));

        private static string Hash(string pin, string salt)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(salt + ":" + pin)));

        // ---------- gates ----------
        // Every "open Nova" / "open Scan Sky" tap goes through one of these:
        // true means go ahead, false means the feature is off and the user
        // has just been told so.

        public static async Task<bool> GateNova()
        {
            if (NovaAllowed) return true;
            await Tell("Ask Nova is switched off by parent mode.");
            return false;
        }

        public static async Task<bool> GateSky()
        {
            if (SkyAllowed) return true;
            await Tell("Scan Sky is switched off by parent mode.");
            return false;
        }

        private static async Task Tell(string message)
        {
            try
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page != null) await page.DisplayAlertAsync("Parent mode", message, "OK");
            }
            catch { }
        }
    }
}
