using UnityEngine;

namespace ThermoTactics
{
    /// <summary>Controls the waves inside a level: spawning and the turn count of the current wave.</summary>
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private GridManager _grid;
        [SerializeField] private UnitFactory _factory;

        private LevelData _level;

        public int WaveIndex { get; private set; } = -1;
        public int TurnInWave { get; private set; }
        public int TotalWaves => _level != null ? _level.waves.Length : 0;
        public int WaveNumber => WaveIndex + 1;
        public WaveData CurrentWave => _level != null && WaveIndex >= 0 && WaveIndex < _level.waves.Length ? _level.waves[WaveIndex] : null;
        public bool HasNextWave => _level != null && WaveIndex + 1 < _level.waves.Length;
        public bool IsCurrentWaveCleared => _grid.EnemyCount == 0;

        /// <summary>Number of waves fully cleared so far.</summary>
        public int WavesCleared => Mathf.Clamp(IsCurrentWaveCleared ? WaveIndex + 1 : WaveIndex, 0, TotalWaves);

        public void Begin(LevelData level)
        {
            _level = level;
            WaveIndex = -1;
            TurnInWave = 0;
        }

        /// <summary>Spawns the next wave into random free enemy slots.</summary>
        public void SpawnNextWave()
        {
            if (!HasNextWave) return;
            WaveIndex++;
            TurnInWave = 1;

            var free = _grid.FreeEnemyLanes();
            foreach (UnitData enemy in CurrentWave.enemies)
            {
                if (enemy == null || free.Count == 0) continue;
                int pick = Random.Range(0, free.Count);
                int lane = free[pick];
                free.RemoveAt(pick);
                int hp = Mathf.Max(1, Mathf.RoundToInt(enemy.maxHp * _level.enemyHpScale));
                _grid.Place(_factory.Spawn(enemy, lane, hp), lane);
            }
        }

        public void AdvanceTurn() => TurnInWave++;
    }
}
