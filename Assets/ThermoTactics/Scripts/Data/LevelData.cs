using UnityEngine;

namespace ThermoTactics
{
    /// <summary>One playable level: its waves, the units the player may deploy and the climate rules.</summary>
    [CreateAssetMenu(menuName = "Thermo-Tactics/Level Data", fileName = "LevelData")]
    public class LevelData : ScriptableObject
    {
        /// <summary>A level offers at most this many unit types in the deploy bar.</summary>
        public const int MaxRosterTypes = 5;

        public string levelName = "Stage 1 · Baguio — Kennon Road";

        [Header("Level select")]
        [Min(1)] public int stageId = 1;
        [Min(1)] public int levelNumber = 1;
        [Tooltip("Place name, e.g. \"Strawberry Farm\" (HUD title and level details).")]
        public string locationName = "Kennon Road";
        [Tooltip("Short caption under the node on the level path, e.g. \"La Trinidad\".")]
        public string pathCaption = "Kennon Road";
        [Tooltip("One-line description under the level name.")]
        public string subtitle = "The zigzag climb into the mountains";
        [TextArea] public string note = "";
        [Range(1, 10)] public int threat = 1;
        [Tooltip("Level 5: the elite difficulty spike (diamond node on the path).")]
        public bool isEliteSpike;
        [Tooltip("Level 10: the stage boss level.")]
        public bool isBossLevel;

        [Header("Climate rules (°C)")]
        public float startingTemperature = 1.0f;
        [Tooltip("The player loses as soon as the temperature reaches this value.")]
        public float lossThreshold = 1.5f;
        [Tooltip("Added when an enemy attacks a lane that has no ally.")]
        public float heatPerUndefendedHit = 0.10f;
        [Tooltip("Removed when an ally attacks a lane that has no enemy (at most once per ally phase).")]
        public float coolingPerEmptyLaneAttack = 0.05f;

        [Header("Oxygen punishment")]
        [Tooltip("OP removed from the wave allowance, indexed by turn number inside the wave (turn 1 = index 1).")]
        public int[] opPenaltyByTurn = { 0, 0, 1, 2, 4, 8 };

        [Header("Content")]
        [Tooltip("Unit types in the deploy bar (at most 5). Any type can fill any of the 6 lanes.")]
        public UnitData[] allyRoster = new UnitData[0];
        public WaveData[] waves = new WaveData[0];
        [Tooltip("Multiplies every enemy's max HP in this level (stage tuning without changing UnitData). Bosses are not scaled.")]
        [Range(0.25f, 2f)] public float enemyHpScale = 0.75f;

        public int GetPenalty(int turnInWave)
        {
            if (opPenaltyByTurn == null || opPenaltyByTurn.Length == 0) return 0;
            return opPenaltyByTurn[Mathf.Clamp(turnInWave, 0, opPenaltyByTurn.Length - 1)];
        }

        /// <summary>Enemy HP in this level (bosses keep their own HP).</summary>
        public int ScaledHp(UnitData enemy)
        {
            if (enemy is BossData) return enemy.maxHp;
            return Mathf.Max(1, Mathf.RoundToInt(enemy.maxHp * enemyHpScale));
        }

        /// <summary>"5·7·10": the Oxygen allowance of each wave.</summary>
        public string OxygenSummary()
        {
            var parts = new string[waves.Length];
            for (int i = 0; i < waves.Length; i++) parts[i] = waves[i] != null ? waves[i].opAllowance.ToString() : "?";
            return string.Join("·", parts);
        }

        private void OnValidate()
        {
            lossThreshold = Mathf.Max(lossThreshold, startingTemperature + 0.05f);
            if (allyRoster != null && allyRoster.Length > MaxRosterTypes)
                Debug.LogWarning($"{name}: a level offers at most {MaxRosterTypes} unit types (has {allyRoster.Length}).", this);
        }
    }
}
