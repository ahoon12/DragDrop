using System;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace DragDrop
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static Harmony harmony;
        public static bool IsEnabled = false;
        public static Settings settings;

        static UnityModManager.ModEntry entry;

        public static void Setup(UnityModManager.ModEntry modEntry)
        {
            entry = modEntry;
            Logger = modEntry.Logger;
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

            modEntry.OnToggle = OnToggle;
            modEntry.OnUpdate = OnUpdate;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.requireConfirmOnLevelLoaded = GUILayout.Toggle(
                settings.requireConfirmOnLevelLoaded,
                Loc.T("레벨 로드 알림 확인 후 닫기", "Close Level Load Notification After Confirmation"));
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            IsEnabled = value;

            if (value)
            {
                harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            }
            else
            {
                DragDropHook.Unhook();
                if (harmony != null)
                    harmony.UnpatchAll(modEntry.Info.Id);
            }

            return true;
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            if (!IsEnabled)
                return;

            DragDropHook.TryHook();
        }

        public static void Fatal(Exception ex)
        {
            if (Logger != null)
                Logger.Error("Unhandled error - Type: " + ex.GetType().FullName + ", Code: " + ex.HResult);

            if (entry != null)
                entry.Active = false;
        }
    }
}
