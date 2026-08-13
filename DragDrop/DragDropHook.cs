using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DragDrop
{
    public static class DragDropHook
    {
        const string TargetTypeName = "FileDragAndDrop";
        const string CallbackMemberName = "OnFilesDropped";

        static GameObject hookObject;
        static bool searchStarted;
        static bool forcedLoadAttempted;
        static int attemptCount;

        public static void TryHook()
        {
            if (hookObject != null)
                return;

            if (!searchStarted)
            {
                searchStarted = true;
                if (Main.Logger != null)
                    Main.Logger.Log("Searching for type " + TargetTypeName + "...");
            }

            attemptCount++;

            try
            {
                Type dragDropType = FindFileDragAndDropType();
                if (dragDropType == null)
                {
                    if (attemptCount == 60)
                    {
                        if (Main.Logger != null)
                            Main.Logger.Error("Could not find type " + TargetTypeName);
                    }
                    return;
                }

                if (Main.Logger != null)
                    Main.Logger.Log("Found type " + TargetTypeName + ": " + dragDropType.AssemblyQualifiedName);

                GameObject obj = new GameObject("DragDrop_Hook");
                UnityEngine.Object.DontDestroyOnLoad(obj);
                Component comp = obj.AddComponent(dragDropType);

                MethodInfo handlerMethod = typeof(DragDropHook).GetMethod(
                    "OnFilesDropped", BindingFlags.Static | BindingFlags.NonPublic);

                if (!TryAssignCallback(dragDropType, comp, handlerMethod))
                {
                    UnityEngine.Object.Destroy(obj);
                    throw new MissingMemberException(TargetTypeName, CallbackMemberName);
                }

                hookObject = obj;
                if (Main.Logger != null)
                    Main.Logger.Log("Drag & drop hook installed.");
            }
            catch (Exception ex)
            {
                Main.Fatal(ex);
            }
        }

        public static void Unhook()
        {
            if (hookObject != null)
            {
                UnityEngine.Object.Destroy(hookObject);
                hookObject = null;
            }
        }

        static Type FindFileDragAndDropType()
        {
            Type t = SearchLoadedAssemblies();
            if (t != null)
                return t;

            if (!forcedLoadAttempted)
            {
                forcedLoadAttempted = true;

                try
                {
                    AssemblyName[] refs = typeof(ADOBase).Assembly.GetReferencedAssemblies();
                    for (int i = 0; i < refs.Length; i++)
                    {
                        try { Assembly.Load(refs[i]); }
                        catch { }
                    }
                }
                catch { }

                t = SearchLoadedAssemblies();
            }

            return t;
        }

        static Type SearchLoadedAssemblies()
        {
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                try
                {
                    Type t = asms[i].GetType(TargetTypeName);
                    if (t != null)
                        return t;
                }
                catch { }
            }
            return null;
        }

        static bool TryAssignCallback(Type dragDropType, object instance, MethodInfo handlerMethod)
        {
            FieldInfo field = dragDropType.GetField(CallbackMemberName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(instance, Delegate.CreateDelegate(field.FieldType, handlerMethod));
                return true;
            }

            PropertyInfo prop = dragDropType.GetProperty(CallbackMemberName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(instance, Delegate.CreateDelegate(prop.PropertyType, handlerMethod), null);
                return true;
            }

            return false;
        }

        static void OnFilesDropped(string[] filesPath)
        {
            try
            {
                if (!Main.IsEnabled)
                    return;

                if (!ADOBase.isLevelEditor)
                    return;

                if (filesPath == null || filesPath.Length == 0)
                    return;

                if (Main.Logger != null)
                    Main.Logger.Log("Files dropped detected (" + filesPath.Length + ")");

                string targetPath = null;
                for (int i = 0; i < filesPath.Length; i++)
                {
                    string clean = SanitizePath(filesPath[i]);
                    string ext = GetExtensionLower(clean);

                    if (GCS.levelExtensions.Contains(ext))
                    {
                        targetPath = clean;
                        break;
                    }
                }

                scnEditor editor = ADOBase.editor;
                if (targetPath == null)
                {
                    editor.ShowNotificationPopup(Loc.T(
                        "지원하지 않는 파일 형식입니다. (.adofai, .zip, .adozip만 가능합니다!)",
                        "Unsupported file format. (Only .adofai, .zip, and .adozip are supported!)"));
                    return;
                }

                editor.StartCoroutine(LoadDroppedLevel(targetPath));
            }
            catch (Exception ex)
            {
                Main.Fatal(ex);
            }
        }

        static IEnumerator LoadDroppedLevel(string path)
        {
            scnEditor editor = ADOBase.editor;
            if (editor == null)
                yield break;

            string ext = GetExtensionLower(path);

            if (GCS.levelZipExtensions.Contains(ext))
            {
                PackageInstallerResult<bool> zipCheck = AdoPackageInstaller.CheckFileIsZip(path);
                if (!zipCheck.IsSuccess)
                {
                    editor.ShowNotificationPopup(zipCheck.Error);
                    yield break;
                }

                string extractDir = RDUtils.GetAvailableDirectoryName(
                    Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)));
                RDDirectory.CreateDirectory(extractDir);

                try
                {
                    ZipUtils.Unzip(path, extractDir);
                }
                catch (Exception ex)
                {
                    editor.ShowNotificationPopup(RDString.Get("editor.notification.unzipFailed"));
                    Main.Fatal(ex);
                    yield break;
                }

                PackageInstallerResult<string> found = AdoPackageInstaller.FindLevelFile(extractDir);
                if (!found.IsSuccess)
                {
                    editor.ShowNotificationPopup(RDString.Get("editor.notification.levelNotFound"));
                    yield break;
                }

                path = found.Value;
            }

            yield return null;

            editor.CheckUnsavedChanges(delegate
            {
                editor.OpenLevel(path);
            });
        }

        static string SanitizePath(string raw)
        {
            return Uri.UnescapeDataString(raw.Replace("file:", ""));
        }

        static string GetExtensionLower(string path)
        {
            string ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext))
                return "";
            return ext.TrimStart('.').ToLowerInvariant();
        }
    }
}
