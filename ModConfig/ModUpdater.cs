using System;
using System.Collections;
using System.Linq;
using Silk;
using UnityEngine;
using Logger = Silk.Logger;

namespace StatsMod
{
    // Stats is now a part of SpiderSurge, so this last version points there instead of checking for updates
    public static class ModUpdater
    {
        private const string SpiderSurgeAssembly = "SpiderSurge";
        private const string SpiderSurgeReleasesUrl = "https://github.com/Dylan-Grinboju/SpiderSurge/releases/latest";
        private const float NoticeDelaySeconds = 15f;
        private static readonly Version FirstSpiderSurgeWithStats = new Version(1, 0, 0);

        public static IEnumerator ShowMovedNotice()
        {
            yield return new WaitForSecondsRealtime(NoticeDelaySeconds);

            // SpiderSurge 1.0.0 and later already ask to delete this file
            if (SpiderSurgeHasStats())
            {
                Logger.LogInfo("SpiderSurge with Stats is installed, it asks to remove Stats Mod");
                yield break;
            }

            Announcer.TwoOptionsPopup(
                "Stats Mod is now part of SpiderSurge,\n" +
                "and this is its last version.\n" +
                "Open the SpiderSurge download page?",
                "Yes", "No",
                OpenDownloadPage,
                () => Logger.LogInfo("Moving to SpiderSurge declined by user."),
                null
            );
        }

        private static bool SpiderSurgeHasStats()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetName())
                .Any(name => name.Name == SpiderSurgeAssembly && name.Version >= FirstSpiderSurgeWithStats);
        }

        private static void OpenDownloadPage()
        {
            try
            {
                Logger.LogInfo("Opening the SpiderSurge download page...");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = SpiderSurgeReleasesUrl,
                    UseShellExecute = true
                });

                Announcer.InformationPopup(
                    "To switch to SpiderSurge:\n" +
                    "1. Download SpiderSurge.dll\n" +
                    "2. Put it in your <Game_Path>/Silk/Mods folder\n" +
                    "3. Delete StatsMod.dll from that folder\n" +
                    "4. Restart SpiderHeck"
                );
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to open download page: {ex.Message}");
                Announcer.InformationPopup("Could not open the download page automatically. Please visit:\nhttps://github.com/Dylan-Grinboju/SpiderSurge/releases");
            }
        }
    }
}
