using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// QA tool: while in Play mode, switches the Game view to phone and tablet screen shapes and saves a screenshot
    /// of each to Logs/qa/sizes/ (16:9 phone 1920x1080, 20:9 phone 2400x1080, 4:3 tablet 2048x1536), then puts the
    /// Game view back. Menu: Thermo-Tactics ▸ Test ▸ Screen Size Shots. Editor only; not part of the game.
    /// </summary>
    public static class ScreenSizeShots
    {
        public const string OutDir = "Logs/qa/sizes";
        private static readonly (string name, int w, int h)[] Sizes =
        {
            ("16x9_1920x1080", 1920, 1080), ("20x9_2400x1080", 2400, 1080), ("4x3_2048x1536", 2048, 1536),
        };

        [MenuItem("Thermo-Tactics/Test/Screen Size Shots")]
        public static void CaptureMenu() => Capture("screen");

        /// <summary>Captures the current screen at every size, files named "{prefix}_{size}.png".</summary>
        public static void Capture(string prefix)
        {
            if (!EditorApplication.isPlaying) { Debug.LogWarning("[Sizes] Enter Play mode first."); return; }
            Directory.CreateDirectory(OutDir);
            EditorWindow view = GameView();
            int original = SelectedIndex(view);
            int step = 0, wait = 0;
            string pending = null;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                view.Repaint();
                if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; return; }
                if (pending != null)   // wait until the screenshot file is written
                {
                    if (!File.Exists(pending) && wait++ < 600) return;
                    pending = null; step++; wait = 0;
                }
                if (step >= Sizes.Length)
                {
                    EditorApplication.update -= tick;
                    Select(view, original);
                    Debug.Log($"[Sizes] Saved {prefix} at {Sizes.Length} screen shapes to {OutDir}.");
                    return;
                }
                var size = Sizes[step];
                if (wait == 0) Select(view, IndexFor(size.w, size.h, size.name));
                wait++;
                bool sized = Screen.width == size.w && Screen.height == size.h;
                if ((!sized && wait < 600) || wait < 30) return;   // the new size, then a moment for layout and camera fit
                string file = Path.Combine(OutDir, $"{prefix}_{size.name}.png");
                if (File.Exists(file)) File.Delete(file);
                ScreenCapture.CaptureScreenshot(file);
                pending = file; wait = 0;
            };
            EditorApplication.update += tick;
        }

        /// <summary>Locks the Game view to a fixed resolution (e.g. 1280 x 720 for manuscript figures).</summary>
        public static void SetFixed(int w, int h) => Select(GameView(), IndexFor(w, h, $"{w}x{h}"));

        /// <summary>Saves one screenshot of the Game view as it is now.</summary>
        public static void Shot(string file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            if (File.Exists(file)) File.Delete(file);
            ScreenCapture.CaptureScreenshot(file);
            GameView().Repaint();
        }

        // ---- Game view size plumbing (internal Unity editor types, reached by reflection) ----
        private static readonly Assembly EditorAsm = typeof(Editor).Assembly;
        private static readonly Type GameViewType = EditorAsm.GetType("UnityEditor.GameView");
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static EditorWindow GameView() => EditorWindow.GetWindow(GameViewType, false, null, false);

        private static int SelectedIndex(EditorWindow view) => (int)GameViewType.GetProperty("selectedSizeIndex", Any).GetValue(view);

        private static void Select(EditorWindow view, int index)
        {
            MethodInfo m = GameViewType.GetMethod("SizeSelectionCallback", Any);
            if (m != null) m.Invoke(view, new object[] { index, null });
            else GameViewType.GetProperty("selectedSizeIndex", Any).SetValue(view, index);
        }

        private static object Group()
        {
            Type sizesType = EditorAsm.GetType("UnityEditor.GameViewSizes");
            Type single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object instance = single.GetProperty("instance").GetValue(null);
            return sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { (int)GameViewSizeGroupTypeOf() });
        }

        private static GameViewSizeGroupType GameViewSizeGroupTypeOf() =>
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? GameViewSizeGroupType.Android : GameViewSizeGroupType.Standalone;

        /// <summary>Index of a fixed-resolution size in the Game view list, adding it (as "TT …") if missing.</summary>
        private static int IndexFor(int w, int h, string label)
        {
            object group = Group();
            Type groupType = group.GetType();
            int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            MethodInfo get = groupType.GetMethod("GetGameViewSize");
            for (int i = 0; i < total; i++)
            {
                object size = get.Invoke(group, new object[] { i });
                Type st = size.GetType();
                if ((int)st.GetProperty("width").GetValue(size) == w && (int)st.GetProperty("height").GetValue(size) == h &&
                    st.GetProperty("sizeType").GetValue(size).ToString() == "FixedResolution")
                    return i;
            }
            Type sizeType = EditorAsm.GetType("UnityEditor.GameViewSize");
            Type kind = EditorAsm.GetType("UnityEditor.GameViewSizeType");
            object fixedRes = Enum.Parse(kind, "FixedResolution");
            object created = Activator.CreateInstance(sizeType, fixedRes, w, h, "TT " + label);
            groupType.GetMethod("AddCustomSize").Invoke(group, new[] { created });
            return total;
        }
    }
}
