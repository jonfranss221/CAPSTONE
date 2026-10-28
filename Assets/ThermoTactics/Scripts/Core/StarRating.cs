namespace ThermoTactics
{
    /// <summary>
    /// 1–3 stars for a won level, rated by the HIGHEST temperature reached during the level
    /// (the final temperature is not used: allies cool the planet back down by the end).
    /// The best stars per level are kept in level_progress.best_stars.
    /// </summary>
    public static class StarRating
    {
        public const int MaxStars = 3;
        public const float ThreeStarAt = 1.20f;   // peak at or below this: 3 stars
        public const float TwoStarAt = 1.35f;     // peak at or below this: 2 stars; any win is at least 1

        public static int For(float peakTemperature)
            => peakTemperature <= ThreeStarAt + 0.0001f ? 3
             : peakTemperature <= TwoStarAt + 0.0001f ? 2
             : 1;

        /// <summary>Best stars on a level: 0 if never won. Wins recorded before stars existed count as 1.</summary>
        public static int For(LevelProgress p)
            => p == null || p.IsCleared != 1 ? 0 : System.Math.Max(1, System.Math.Min(MaxStars, p.BestStars));

        /// <summary>True when a win beats the stars of an earlier win on the same level.</summary>
        public static bool IsNewBest(bool won, int stars, LevelProgress before)
            => won && before != null && before.IsCleared == 1 && stars > For(before);

        /// <summary>What the next star needs, for the end screen and level card.</summary>
        public static string NextStarHint(int stars)
            => stars >= 3 ? "All 3 stars earned!"
             : stars == 2 ? $"Keep the temperature at {ThreeStarAt:0.00} °C or lower all level for 3 stars."
             : $"Keep the temperature at {TwoStarAt:0.00} °C or lower for 2 stars, {ThreeStarAt:0.00} °C for 3.";
    }
}
