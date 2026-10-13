using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>One unit on the board in a saved game.</summary>
    [Serializable]
    public class SavedUnit
    {
        public int lane;
        public string unitId;
        public int hp;
        public int maxHp;
    }

    /// <summary>
    /// Everything needed to put a level back exactly where the player left it:
    /// the level, the wave and turn, the Oxygen still unspent, the temperature and every unit with its HP.
    /// Stored as one row of the saved_session table (one row per player). Written at the start of every
    /// player turn, after every deploy, and when the app is paused or closed; deleted when the level ends.
    /// </summary>
    [Serializable]
    public class SessionSnapshot
    {
        public string playerName;
        public int stageId;
        public int levelNumber;
        public int waveNumber;      // 1-based
        public int turnNumber;      // turn inside the wave, 1-based
        public int oxygenLeft;
        public int oxygenAllowance;
        public int oxygenPenalty;
        public float temperature;
        public int bossTurn;        // boss turns taken so far (0 when no boss)
        public float peakTemperature; // highest °C reached so far in this level (rates the stars)
        public List<SavedUnit> allies = new List<SavedUnit>();
        public List<SavedUnit> enemies = new List<SavedUnit>();
        public string savedAt;      // yyyy-MM-dd HH:mm:ss
        public string appVersion;

        [Serializable]
        private class UnitList { public List<SavedUnit> units = new List<SavedUnit>(); }

        /// <summary>JSON stored in the allies / enemies columns.</summary>
        public static string ToJson(List<SavedUnit> units) => JsonUtility.ToJson(new UnitList { units = units ?? new List<SavedUnit>() });

        public static List<SavedUnit> FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new List<SavedUnit>();
            try { return JsonUtility.FromJson<UnitList>(json)?.units ?? new List<SavedUnit>(); }
            catch (Exception) { return new List<SavedUnit>(); }
        }

        /// <summary>"Saved Oct 12, 9:42 PM" (falls back to the raw text).</summary>
        public string FriendlySavedAt()
        {
            if (DateTime.TryParseExact(savedAt, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime t))
                return t.ToString("MMM d, h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            return savedAt;
        }
    }
}
