using UnityEditor;
using UnityEditor.SceneManagement;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Pressing Play in the editor always starts from the Main Menu, whichever scene is open
    /// (Build Everything leaves the battle scene open). Turn it off under Thermo-Tactics ▸ Always Play From Main Menu
    /// to play the open scene directly.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMenu
    {
        private const string MenuPath = "Thermo-Tactics/Always Play From Main Menu";
        private const string PrefKey = "tt.playFromMenu";

        static PlayFromMenu() => EditorApplication.delayCall += Apply;

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        private static void Apply()
        {
            EditorSceneManager.playModeStartScene = Enabled
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuSceneBuilder.ScenePath)
                : null;
        }

        [MenuItem(MenuPath, false, 100)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }
    }
}
