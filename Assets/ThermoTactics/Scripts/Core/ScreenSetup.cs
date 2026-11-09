using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Keeps the game in landscape on phones. The UI is laid out for a 1280 × 720 landscape screen,
    /// so portrait would show only a narrow slice of it. The player can still turn the phone either way
    /// (landscape left or right); portrait is never used, even when the phone's auto-rotate is off.
    /// Runs once before the first scene loads. The same rule is set in Player Settings by ReleaseBuilder.
    /// </summary>
    public static class ScreenSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LockLandscape()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
