namespace ThermoTactics
{
    /// <summary>
    /// Saves and reads level progress for the game logic, so GameManager no longer calls the database
    /// itself at the end of a level. All SQL stays in ResultsDatabase; this class decides which tables a
    /// finished level touches and returns the stored rows as models. It applies no game rules.
    /// </summary>
    public class ProgressService
    {
        private readonly ResultsDatabase _database;

        public ProgressService(ResultsDatabase database)
        {
            _database = database;
        }

        /// <summary>
        /// Stores one finished level: removes the Continue save, records the attempt in level_progress and
        /// adds a game_results row. Returns the stored record and the stage progress read back afterwards.
        /// </summary>
        public SavedLevelResult SaveLevelResult(LevelResult r)
        {
            int stageId = r.Stage.stageId;
            LevelProgress before = GetStageSummary(r.PlayerName, r.Stage).Find(r.LevelNumber);

            _database.DeleteSession(r.PlayerName);   // the level is over, so there is nothing to Continue
            _database.RecordAttempt(r.PlayerName, stageId, r.LevelNumber, r.Won, r.FinalTemperature, r.Stars);
            GameResult record = _database.Insert(r.PlayerName, r.Won, r.FinalTemperature, r.WavesCleared);

            StageSummary stage = GetStageSummary(r.PlayerName, r.Stage);
            return new SavedLevelResult
            {
                Record = record,
                Saved = record != null && record.id > 0,
                Before = before,
                After = stage.Find(r.LevelNumber),
                Stage = stage,
            };
        }

        /// <summary>Cleared levels and stars of one stage for a player.</summary>
        public StageSummary GetStageSummary(string playerName, StageData stage)
        {
            var summary = new StageSummary { StageId = stage.stageId, Levels = stage.LevelCount };
            foreach (LevelProgress p in _database.GetProgress(playerName, stage.stageId))
                summary.Add(p);
            return summary;
        }

        /// <summary>Adds Carbon Credits to the player's wallet and returns the new balance (the old one if nothing was added or the write failed).</summary>
        public int AwardCredits(string playerName, int amount)
        {
            if (amount > 0)
            {
                int balance = _database.AddCredits(playerName, amount);
                if (balance >= 0) return balance;
            }
            return _database.GetCredits(playerName);
        }
    }
}
