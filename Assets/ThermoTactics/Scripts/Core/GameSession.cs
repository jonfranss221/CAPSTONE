namespace ThermoTactics
{
    /// <summary>
    /// Carries the player's choice from the Main Menu scene to the battle scene (and back).
    /// Static, so it survives the scene load; nothing here is saved to disk.
    /// </summary>
    public static class GameSession
    {
        public const string MenuScene = "MainMenu";
        public const string BattleScene = "Baguio_Battle";

        public static string PlayerName = "";
        public static StageData Stage;
        public static int LevelNumber = 1;

        /// <summary>Set when the player pressed Continue: the battle scene rebuilds this state.</summary>
        public static SessionSnapshot Resume;

        /// <summary>The menu opens on the level path instead of the title (after "Level Select" in battle).</summary>
        public static bool OpenLevelSelect;

        /// <summary>The menu opens on the stage cards (after "Stage Select" in battle).</summary>
        public static bool OpenStageSelect;

        public static void Play(string player, StageData stage, int level, SessionSnapshot resume = null)
        {
            PlayerName = player;
            Stage = stage;
            LevelNumber = level;
            Resume = resume;
        }
    }
}
