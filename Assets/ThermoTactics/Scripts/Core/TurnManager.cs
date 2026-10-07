using System.Collections;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Runs one turn after the player taps End Turn:
    /// ally attacks → (wave cleared?) → enemy attacks → temperature check → next turn.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        [SerializeField] private GridManager _grid;
        [SerializeField] private WaveManager _waves;
        [SerializeField] private OxygenPointSystem _oxygen;
        [SerializeField] private TemperatureSystem _temperature;
        [SerializeField] private LaneController _lanes;
        [SerializeField] private UIManager _ui;

        public bool IsResolving { get; private set; }

        public void EndTurn()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.CanPlayerAct) return;
            StartCoroutine(ResolveTurn());
        }

        /// <summary>Gives the player the OP for the current turn of the current wave.</summary>
        public void BeginPlayerTurn(LevelData level)
        {
            _oxygen.StartTurn(_waves.CurrentWave.opAllowance, level.GetPenalty(_waves.TurnInWave));
            _grid.SetDeployHighlights(true);
            _ui.RefreshHud();
        }

        private IEnumerator ResolveTurn()
        {
            IsResolving = true;
            var gm = GameManager.Instance;
            _grid.SetDeployHighlights(false);
            _ui.RefreshHud();

            // 1. Allies attack the enemy in their lane (or cool an empty lane).
            yield return RunPhase(Faction.Ally);
            yield return new WaitForSeconds(0.25f);

            // 2. Wave cleared? Then enemies do not get to act.
            if (_waves.IsCurrentWaveCleared)
            {
                yield return HandleWaveCleared(gm);
                IsResolving = false;
                yield break;
            }

            // 3. Enemies attack the ally in their lane (or heat an undefended lane).
            yield return RunPhase(Faction.Enemy);
            yield return new WaitForSeconds(0.25f);

            // 4. Climate check: reaching the threshold ends the level.
            if (_temperature.HasReachedThreshold)
            {
                IsResolving = false;
                gm.EndLevel(false);
                yield break;
            }

            // 5. Next turn of the same wave, with the OP punishment.
            _waves.AdvanceTurn();
            BeginPlayerTurn(gm.Level);
            if (_oxygen.Penalty > 0)
                _ui.Announce($"Turn {_waves.TurnInWave}: the wave is dragging on. Oxygen −{_oxygen.Penalty}.", UIManager.Warning);
            IsResolving = false;
            _ui.RefreshHud();
        }

        private IEnumerator HandleWaveCleared(GameManager gm)
        {
            if (!_waves.HasNextWave)
            {
                gm.EndLevel(true);
                yield break;
            }
            _ui.Announce($"Wave {_waves.WaveNumber} cleared!", UIManager.Good);
            yield return new WaitForSeconds(1.2f);
            _waves.SpawnNextWave();
            BeginPlayerTurn(gm.Level);
            _ui.Announce($"Wave {_waves.WaveNumber} of {_waves.TotalWaves} incoming! +{_waves.CurrentWave.opAllowance} Oxygen each turn.", UIManager.Info);
        }

        /// <summary>All units of one side attack at the same time; waits until every animation finishes.</summary>
        private IEnumerator RunPhase(Faction side)
        {
            int running = 0;
            for (int lane = 0; lane < _grid.LaneCount; lane++)
            {
                UnitBase attacker = side == Faction.Ally ? _grid.GetAlly(lane) : _grid.GetEnemy(lane);
                if (attacker == null || attacker.IsDead) continue;
                UnitBase target = side == Faction.Ally ? _grid.GetEnemy(lane) : _grid.GetAlly(lane);
                running++;
                StartCoroutine(Track(Attack(attacker, target, lane, side), () => running--));
            }
            while (running > 0) yield return null;
        }

        private IEnumerator Attack(UnitBase attacker, UnitBase target, int lane, Faction side)
        {
            bool hadTarget = target != null && !target.IsDead;
            yield return attacker.AttackRoutine(() =>
            {
                if (hadTarget && target != null)
                {
                    target.TakeDamage(attacker.Damage);
                    _ui.ShowFloatingText(target.TopPosition, $"-{attacker.Damage}", new Color(1f, 0.25f, 0.2f));
                    if (target.IsDead) _grid.Remove(target);
                }
                else if (side == Faction.Ally)
                {
                    _lanes.ReportAllyAttack(lane, false, attacker.TopPosition);
                }
                else
                {
                    _lanes.ReportEnemyAttack(lane, false, attacker.TopPosition);
                }
                _ui.RefreshHud();
            });
        }

        private static IEnumerator Track(IEnumerator routine, System.Action done)
        {
            yield return routine;
            done();
        }
    }
}
