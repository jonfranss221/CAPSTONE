using UnityEngine;

namespace ThermoTactics
{
    /// <summary>One playable level: its waves, the units the player may deploy and the climate rules.</summary>
    [CreateAssetMenu(menuName = "Thermo-Tactics/Level Data", fileName = "LevelData")]
    public class LevelData : ScriptableObject
    {
        public string levelName = "Stage 1 · Baguio — Kennon Road";

        [Header("Climate rules (°C)")]
        public float startingTemperature = 1.0f;
        [Tooltip("The player loses as soon as the temperature reaches this value.")]
        public float lossThreshold = 1.5f;
        [Tooltip("Added when an enemy attacks a lane that has no ally.")]
        public float heatPerUndefendedHit = 0.10f;
        [Tooltip("Removed when an ally attacks a lane that has no enemy.")]
        public float coolingPerEmptyLaneAttack = 0.05f;

        [Header("Oxygen punishment")]
        [Tooltip("OP removed from the wave allowance, indexed by turn number inside the wave (turn 1 = index 1).")]
        public int[] opPenaltyByTurn = { 0, 0, 1, 2, 4, 8 };

        [Header("Content")]
        public UnitData[] allyRoster = new UnitData[0];
        public WaveData[] waves = new WaveData[0];
        [Tooltip("Multiplies every enemy's max HP in this level (stage tuning without changing UnitData).")]
        [Range(0.25f, 2f)] public float enemyHpScale = 0.75f;

        public int GetPenalty(int turnInWave)
        {
            if (opPenaltyByTurn == null || opPenaltyByTurn.Length == 0) return 0;
            return opPenaltyByTurn[Mathf.Clamp(turnInWave, 0, opPenaltyByTurn.Length - 1)];
        }

        private void OnValidate()
        {
            lossThreshold = Mathf.Max(lossThreshold, startingTemperature + 0.05f);
        }
    }
}
