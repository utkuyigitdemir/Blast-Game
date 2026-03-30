using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BlastGame.Editor
{
    /// <summary>
    /// Automatically adds a 9:16 portrait resolution to the Game view
    /// and selects it once per editor session.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoPortraitGameView
    {
        private const string SizeName = "9:16 Portrait";
        private const int Width = 1080;
        private const int Height = 1920;
        private const string SessionKey = "AutoPortraitGameView_Set";

        static AutoPortraitGameView()
        {
            if (!SessionState.GetBool(SessionKey, false))
            {
                EditorApplication.delayCall += ApplyPortraitSize;
            }
        }

        private static void ApplyPortraitSize()
        {
            try
            {
                int index = FindOrAddSize();
                if (index >= 0)
                {
                    SelectSize(index);
                    SessionState.SetBool(SessionKey, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "[AutoPortraitGameView] Could not set portrait mode automatically. " +
                    "Please set Game view to 9:16 manually.\n" + e.Message);
            }
        }

        private static int FindOrAddSize()
        {
            var asm = typeof(UnityEditor.Editor).Assembly;

            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var groupType = asm.GetType("UnityEditor.GameViewSizeGroup");
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var sizeTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");

            // Singleton instance
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object instance = singletonType.GetProperty("instance").GetValue(null);

            // Current platform group
            object currentGroup = sizesType.GetProperty("currentGroupType").GetValue(instance);
            object group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { currentGroup });

            // Search existing sizes
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            var getSizeMethod = groupType.GetMethod("GetGameViewSize");
            var baseTextProp = sizeType.GetProperty("baseText");

            for (int i = 0; i < count; i++)
            {
                object size = getSizeMethod.Invoke(group, new object[] { i });
                string name = (string)baseTextProp.GetValue(size);
                if (name == SizeName)
                    return i;
            }

            // Create and add new size
            object fixedRes = Enum.Parse(sizeTypeEnum, "FixedResolution");
            var ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            object newSize = ctor.Invoke(new object[] { fixedRes, Width, Height, SizeName });

            groupType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
            sizesType.GetMethod("SaveToHDD").Invoke(instance, null);

            return count;
        }

        private static void SelectSize(int index)
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var gameViewType = asm.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);

            var prop = gameViewType.GetProperty(
                "selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop != null)
            {
                prop.SetValue(window, index);
            }
        }
    }
}
