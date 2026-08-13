using HarmonyLib;

namespace DragDrop
{
    [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.ShowNotification))]
    static class LevelLoader
    {
        static bool Prefix(scnEditor __instance, string text)
        {
            if (Main.settings == null || !Main.settings.requireConfirmOnLevelLoaded)
                return true;

            string levelLoadedText = RDString.Get("editor.notification.levelLoaded");
            if (text != levelLoadedText)
                return true;

            __instance.ShowNotificationPopup(text);
            return false;
        }
    }
}
