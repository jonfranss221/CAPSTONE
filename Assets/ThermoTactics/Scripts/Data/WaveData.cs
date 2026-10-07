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
        [Tooltip("Enemies that spawn when the wave starts (max 6, one per lane).")]
        public UnitData[] enemies = new UnitData[0];

        private void OnValidate()
        {
            if (enemies != null && enemies.Length > IsoBoardLayout.LaneCount)
                Debug.LogWarning($"{name}: a wave can have at most {IsoBoardLayout.LaneCount} enemies (one per lane).", this);
        }
    }
}
