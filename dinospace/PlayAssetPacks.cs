using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if ANDROID
using Xamarin.Google.Android.Play.Core.AssetPacks;
#endif

namespace dinospace
{
    // NovaSaur's ~3 GB model ships INSIDE the Play Store install.
    //
    // Google Play caps each asset pack at 1.5 GB, so the model file is split
    // into 1 GB chunks (NovaSaur.litertlm.part1, .part2, ...) and each chunk
    // rides in its own fast-follow asset pack (novamodel1..novamodel4).
    // Google Play downloads those packs automatically the moment the app is
    // installed - before the user even opens the app - so there is no
    // in-app download step anymore.
    //
    // This class is the only place that talks to the Play asset-pack API:
    // it finds the chunk files Play delivered, and frees the packs after
    // the chunks have been joined into the real model file.
    public static class PlayAssetPacks
    {
        public static readonly string[] PackNames = { "novamodel1", "novamodel2", "novamodel3", "novamodel4" };

        // Every chunk file (".part" in the name) found across the delivered
        // packs. Packs still downloading, or never shipped, simply don't
        // contribute files yet.
        public static List<string> FindDeliveredChunkFiles()
        {
            var chunks = new List<string>();
#if ANDROID
            try
            {
                var manager = AssetPackManagerFactory.GetInstance(Android.App.Application.Context);
                foreach (var name in PackNames)
                {
                    try
                    {
                        AssetPackLocation location = manager.GetPackLocation(name);
                        var folder = location?.AssetsPath();
                        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) continue;

                        chunks.AddRange(Directory
                            .GetFiles(folder, "*.part*", SearchOption.AllDirectories));
                    }
                    catch { }
                }
            }
            catch { }
#endif
            return chunks;
        }

        // After the model has been assembled into the app's files folder, the
        // delivered packs are just 3 GB of duplicate data - ask Play to
        // delete them so the app doesn't use double the storage.
        public static void RemoveDeliveredPacks()
        {
#if ANDROID
            try
            {
                var manager = AssetPackManagerFactory.GetInstance(Android.App.Application.Context);
                foreach (var name in PackNames)
                {
                    try { manager.RemovePack(name); } catch { }
                }
            }
            catch { }
#endif
        }
    }
}
