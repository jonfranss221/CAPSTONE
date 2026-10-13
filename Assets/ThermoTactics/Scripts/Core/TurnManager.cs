using System.Collections;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Runs one turn after the player taps End Turn:
    /// ally attacks → (wave cleared?) → enemy attacks (or the boss's cleave / slam) → temperature check → next turn.
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

        /// <summary>Gives the player the OP for the current turn of the current wave, then autosaves.</summary>
        public void BeginPlayerTurn(LevelData level)
        {
            WaveData wave = _waves.CurrentWave;
            int penalty = wave.ignorePenalty ? 0 : level.GetPenalty(_waves.TurnInWave);
            _oxygen.StartTurn(wave.opAllowance, penalty);
            if (_grid.Boss != null) _grid.Boss.LockNextMove();
            _grid.SetDeployHighlights(true);
            _ui.RefreshHud();
            WarnIfSlamComing();
            GameManager.Instance?.Autosave();
        }

        /// <summary>After Continue: back to the player's turn without granting new OP.</summary>
        public void ResumePlayerTurn()
        {
            if (_grid.Boss != null) _grid.Boss.LockNextMove();
            _grid.SetDeployHighlights(true);
            _ui.RefreshHud();
            WarnIfSlamComing();
        }

        private void WarnIfSlamComing()
        {
            BossUnit boss = _grid.Boss;
            if (boss == null || !SettingsStore.BossWarning || boss.TurnsUntilSlam != 1) return;
            _ui.Announce($"Warning: {boss.Data.displayName} will slam ALL 6 lanes this turn! Fill every lane.", UIManager.Warning);
        }

        private IEnumerator ResolveTurn()
        {
            IsResolving = true;
            var gm = GameManager.Instance;
            _grid.SetDeployHighlights(false);
            _ui.RefreshHud();

            // 1. Allies attack the enemy in their lane (or cool an empty lane, at most once per phase).
            _lanes.BeginAllyPhase();
            yield return RunPhase(Faction.Ally);
            yield return new WaitForSeconds(0.25f);

            // 2. Wave cleared? Then enemies do not get to act.
            if (_waves.IsCurrentWaveCleared)
            {
                yield return HandleWaveCleared(gm);
                IsResolving = false;
                yield break;
            }

            // 3. Enemies attack the ally in their lane (or heat an undefended lane). A boss acts once for all lanes.
            if (_grid.Boss != null) yield return BossPhase(_grid.Boss);
            else yield return RunPhase(Faction.Enemy);
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
            IsResolving = false;
            BeginPlayerTurn(gm.Level);
            if (_oxygen.Penalty > 0)
                _ui.Announce($"Turn {_waves.TurnInWave}: the wave is dragging on. Oxygen −{_oxygen.Penalty}.", UIManager.Warning);
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
            IsResolving = false;
            BeginPlayerTurn(gm.Level);
            if (_waves.IsBossWave && _grid.Boss != null)
                _ui.Announce($"FINAL WAVE: the {_grid.Boss.Data.displayName} blocks all 6 lanes! {_waves.CurrentWave.opAllowance} Oxygen each turn, no penalty.", UIManager.Warning);
            else
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
                if (hadTarget && target != null && !target.IsDead)
                {
                    target.TakeDamage(attacker.Damage);
                    // A boss spans every lane: show each hit above the lane that dealt it.
                    Vector3 at = target is BossUnit
                        ? IsoBoardLayout.SlotPosition(lane, true) + Vector3.up * 1.6f
                        : target.TopPosition;
                    _ui.ShowFloatingText(at, $"-{attacker.Damage}", new Color(1f, 0.25f, 0.2f));
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

        /// <summary>The boss's turn: one cleave (target lane ± 1) or one slam (all 6 lanes).</summary>
        private IEnumerator BossPhase(BossUnit boss)
        {
            BossUnit.BossMove move = boss.PlanNext(_grid);
            _ui.ShowBossMove(move, boss);
            yield return boss.PerformRoutine(move.IsSlam, () =>
            {
                foreach (int lane in move.Lanes)
                {
                    UnitBase ally = _grid.GetAlly(lane);
                    Vector3 lanePos = IsoBoardLayout.SlotPosition(lane, false) + Vector3.up * 1.35f;
                    if (ally != null && !ally.IsDead)
                    {
                        ally.TakeDamage(move.Damage);
                        _ui.ShowFloatingText(ally.TopPosition, $"-{move.Damage}", new Color(1f, 0.25f, 0.2f));
                        if (ally.IsDead) _grid.Remove(ally);
                    }
                    else
                    {
                        _lanes.ReportEnemyAttack(lane, false, lanePos);
                    }
                }
                _ui.RefreshHud();
            });
            _ui.RefreshHud();
        }

        private static IEnumerator Track(IEnumerator routine, System.Action done)
        {
            yield return routine;
            done();
        }
    }
}
