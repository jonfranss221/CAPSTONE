using UnityEditor;
using UnityEditor.SceneManagement;

namespace ThermoTactics.EditorTools
{
    /// <summary>Menu Thermo-Tactics ▸ Test: starts the automatic play-test of a stage (see AutoPlaytest).</summary>
    public static class PlaytestMenu
    {
        [MenuItem("Thermo-Tactics/Test/Play Stage 1 Automatically")]
        public static void PlayStage1() => PlayStage(1);

        [MenuItem("Thermo-Tactics/Test/Play Stage 2 Automatically")]
        public static void PlayStage2() => PlayStage(2);

        private static void PlayStage(int stageId)
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(Dev.AutoPlaytest.FlagKey, true);
            SessionState.SetInt(Dev.AutoPlaytest.StageKey, stageId);
            EditorSceneManager.OpenScene(MenuSceneBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Thermo-Tactics/Test/Quick Check of New Screens")]
        public static void QuickCheck()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(Dev.QuickCheck.FlagKey, true);
            EditorSceneManager.OpenScene(MenuSceneBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
