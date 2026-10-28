using System.Collections.Generic;

namespace ThermoTactics
{
    /// <summary>Cleared levels and stars of one stage for one player, built from level_progress rows.</summary>
    public class StageSummary
    {
        public int StageId;
        public int Levels;     // levels in the stage
        public int Cleared;    // levels won at least once
        public int Stars;      // best stars summed over the stage

        public int MaxStars => Levels * StarRating.MaxStars;
        public bool IsComplete => Levels > 0 && Cleared >= Levels;

        private readonly List<LevelProgress> _rows = new List<LevelProgress>();

        /// <summary>The progress row of one level, or null if it was never played.</summary>
        public LevelProgress Find(int levelNumber)
        {
            foreach (LevelProgress p in _rows)
                if (p.LevelNumber == levelNumber) return p;
            return null;
        }

        /// <summary>Adds one level's row and counts it toward the cleared levels and stars.</summary>
        public void Add(LevelProgress p)
        {
            if (p == null) return;
            _rows.Add(p);
            if (p.IsCleared == 1) Cleared++;
            Stars += StarRating.For(p);
        }
    }
}
