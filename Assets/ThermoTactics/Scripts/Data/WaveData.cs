using UnityEngine;

namespace ThermoTactics
{
    /// <summary>One wave: which enemies spawn and how much Oxygen the player gets each turn.</summary>
    [CreateAssetMenu(menuName = "Thermo-Tactics/Wave Data", fileName = "WaveData")]
    public class WaveData : ScriptableObject
    {
        [Min(1)] public int waveNumber = 1;
        [Tooltip("Oxygen Points granted at the start of every turn of this wave (not carried over).")]
        [Min(1)] public int opAllowance = 5;

        [Tooltip("Skip the long-wave Oxygen penalty for this wave (used for boss waves, which are meant to last).")]
        public bool ignorePenalty = false;
        [Tooltip("Enemies that spawn when the wave starts (max 6, one per lane). A BossData here fills all 6 lanes alone.")]
        public UnitData[] enemies = new UnitData[0];

        /// <summary>True when this wave is a boss wave (the boss fights alone).</summary>
        public bool HasBoss
        {
            get { if (enemies != null) foreach (var e in enemies) if (e is BossData) return true; return false; }
        }

        private void OnValidate()
        {
            if (enemies != null && enemies.Length > IsoBoardLayout.LaneCount)
                Debug.LogWarning($"{name}: a wave can have at most {IsoBoardLayout.LaneCount} enemies (one per lane).", this);
        }
    }
}
