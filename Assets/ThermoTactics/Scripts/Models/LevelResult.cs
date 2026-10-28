namespace ThermoTactics
{
    /// <summary>
    /// One finished level, as the game logic hands it to ProgressService. Plain data: the values are
    /// decided by GameManager and StarRating before this is built, and nothing here touches the database.
    /// </summary>
    public class LevelResult
    {
        public string PlayerName;
        public StageData Stage;
        public int LevelNumber;
        public bool Won;
        public float FinalTemperature;
        public float PeakTemperature;    // highest °C reached in the level; rates the stars
        public int WavesCleared;
        public int Stars;                // 0 on a loss
    }

    /// <summary>What ProgressService returns after saving a LevelResult.</summary>
    public class SavedLevelResult
    {
        public GameResult Record;          // the game_results row (id 0 if it could not be written)
        public bool Saved;                 // false when the database could not be written
        public LevelProgress Before;       // this level's progress row before the attempt (null if never played)
        public LevelProgress After;        // this level's progress row after the attempt
        public StageSummary Stage;         // the whole stage after the attempt
    }
}
