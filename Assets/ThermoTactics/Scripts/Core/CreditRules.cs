using UnityEngine;

namespace ThermoTactics
{
    /// <summary>What a win pays, line by line (shown on the Level Complete screen).</summary>
    public struct CreditReward
    {
        public int Base;        // first clear, or the replay share
        public int BossBonus;   // first clear of a stage's last level
        public int StarBonus;   // 20 per star earned for the first time
        public bool Replay;
        public int Total => Base + BossBonus + StarBonus;
    }

    /// <summary>
    /// Carbon Credits (CC), the in-game currency. 1 CC stands for 1 tonne of CO₂ kept out of the air.
    /// First clear: 50 + 10 × level number, +100 on a stage's boss level. Stars: +20 for each star earned for the
    /// first time (going from 1 to 3 stars later pays +40). Replaying a cleared level: 25% of the first-clear base,
    /// so levels cannot be farmed. A loss pays nothing. Baguio, all levels at 3 stars: 1,050 + 600 + 100 = 1,750 CC.
    /// </summary>
    public static class CreditRules
    {
        public const int FirstClearBase = 50;
        public const int PerLevel = 10;
        public const int BossBonus = 100;
        public const int PerNewStar = 20;
        public const float ReplayShare = 0.25f;

        public static int FirstClear(int levelNumber) => FirstClearBase + PerLevel * Mathf.Max(1, levelNumber);

        public static CreditReward ForWin(int levelNumber, bool bossLevel, bool clearedBefore, int bestStarsBefore, int starsNow)
        {
            var r = new CreditReward { Replay = clearedBefore };
            int first = FirstClear(levelNumber);
            if (clearedBefore) r.Base = Mathf.RoundToInt(first * ReplayShare);
            else
            {
                r.Base = first;
                if (bossLevel) r.BossBonus = BossBonus;
            }
            r.StarBonus = PerNewStar * Mathf.Max(0, starsNow - Mathf.Max(0, bestStarsBefore));
            return r;
        }

        /// <summary>"1,750 CC"</summary>
        public static string Format(int credits) => credits.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " CC";
    }
}
