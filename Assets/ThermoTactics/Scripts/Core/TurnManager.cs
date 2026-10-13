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
        [SerializeField] private BattleCamera _camera;

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

        /// <summary>
        /// Resolves the turn one lane at a time, left to right. In each lane the camera zooms in, the ally attacks
        /// (an enemy, or the empty slot to cool the planet), then the enemy, if still standing, attacks (the ally,
        /// or the undefended lane to heat the planet). A lane with no units is shown briefly and skipped.
        /// In the boss wave, allies hit the boss lane by lane, then the boss makes its one move.
        /// </summary>
        private IEnumerator ResolveTurn()
        {
            IsResolving = true;
            var gm = GameManager.Instance;
            _grid.SetDeployHighlights(false);
            _ui.RefreshHud();
            _lanes.BeginAllyPhase();

            bool bossWave = _grid.Boss != null;
            for (int lane = 0; lane < _grid.LaneCount; lane++)
            {
                yield return ResolveLane(lane, bossWave);
                if (_temperature.HasReachedThreshold) break;
                if (bossWave && (_grid.Boss == null || _grid.Boss.IsDead)) break;
            }
            _ui.HideLaneCaption();

            // Loss: the temperature reached the limit during the lanes.
            if (_temperature.HasReachedThreshold)
            {
                yield return CameraHome();
                IsResolving = false;
                gm.EndLevel(false);
                yield break;
            }

            // Wave cleared? Then the next wave comes (or the level is won).
            if (_waves.IsCurrentWaveCleared)
            {
                yield return CameraHome();
                yield return HandleWaveCleared(gm);
                IsResolving = false;
                yield break;
            }

            // The boss acts once for all lanes, after every ally has hit it.
            if (_grid.Boss != null)
            {
                yield return BossPhase(_grid.Boss);
                _ui.HideLaneCaption();
                yield return Wait(0.3f);
            }
            yield return CameraHome();

            if (_temperature.HasReachedThreshold)
            {
                IsResolving = false;
                gm.EndLevel(false);
                yield break;
            }

            // Next turn of the same wave, with the OP punishment.
            _waves.AdvanceTurn();
            IsResolving = false;
            BeginPlayerTurn(gm.Level);
            if (_oxygen.Penalty > 0)
                _ui.Announce($"Turn {_waves.TurnInWave}: the wave is dragging on. Oxygen −{_oxygen.Penalty}.", UIManager.Warning);
            _ui.RefreshHud();
        }

        private IEnumerator ResolveLane(int lane, bool bossWave)
        {
            UnitBase ally = Alive(_grid.GetAlly(lane));
            UnitBase enemy = Alive(_grid.GetEnemy(lane));
            string title = $"LANE {lane + 1}";

            // Nothing fights here: a quick look, then on to the next lane.
            if (ally == null && (enemy == null || bossWave))
            {
                if (!BattleCamera.Enabled || _camera == null) yield break;
                _ui.ShowLaneCaption(title, bossWave ? "No defender here. The boss can hit this lane." : "Empty lane. Nothing happens.", UIManager.Info);
                yield return _camera.FocusLane(lane, bossWave);
                yield return Wait(0.3f);
                yield break;
            }

            _ui.ShowLaneCaption(title, Matchup(ally, enemy, bossWave), UIManager.Info);
            if (_camera != null) yield return _camera.FocusLane(lane, bossWave);
            yield return Wait(0.15f);

            var result = new System.Text.StringBuilder();
            float climate = 0f;

            // 1. The ally attacks.
            if (ally != null)
            {
                bool hadTarget = enemy != null;
                int hpBefore = hadTarget ? enemy.Hp : 0;
                bool coolingUsed = _lanes.CooledThisTurn;
                yield return Attack(ally, enemy, lane, Faction.Ally, d => climate += d);
                if (hadTarget)
                {
                    result.Append($"{ally.Data.displayName} hits {enemy.Data.displayName} for {Mathf.Min(hpBefore, ally.Damage)}");
                    if (enemy.IsDead) result.Append(bossWave ? ". The boss falls!" : $". {enemy.Data.displayName} defeated!");
                }
                else if (climate < 0f) result.Append($"{ally.Data.displayName} attacks the empty lane: the air cools {climate:0.00} °C");
                else if (!coolingUsed) result.Append($"{ally.Data.displayName} attacks the empty lane (already at the {_temperature.Baseline:0.00} °C minimum)");
                else result.Append($"{ally.Data.displayName} attacks the empty lane (cooling counts once per turn)");
            }

            // 2. The enemy, if still standing, attacks (not in the boss wave: the boss moves once after all lanes).
            enemy = bossWave ? null : Alive(_grid.GetEnemy(lane));
            if (enemy != null && !_temperature.HasReachedThreshold)
            {
                if (result.Length > 0) yield return Wait(0.1f);
                UnitBase target = Alive(_grid.GetAlly(lane));
                bool hadTarget = target != null;
                int hpBefore = hadTarget ? target.Hp : 0;
                float heat = 0f;
                yield return Attack(enemy, target, lane, Faction.Enemy, d => heat += d);
                climate += heat;
                if (result.Length > 0) result.Append(" · ");
                if (hadTarget)
                {
                    result.Append($"{enemy.Data.displayName} hits back for {Mathf.Min(hpBefore, enemy.Damage)}");
                    if (target.IsDead) result.Append($". {target.Data.displayName} is down!");
                }
                else result.Append($"{enemy.Data.displayName} attacks the undefended lane: +{heat:0.00} °C");
            }

            Color tone = climate > 0f ? new Color(1f, 0.6f, 0.5f) : climate < 0f ? new Color(0.55f, 0.85f, 1f) : UIManager.Good;
            _ui.ShowLaneCaption(title, result.ToString(), tone);
            yield return Wait(0.45f);
        }

        private static string Matchup(UnitBase ally, UnitBase enemy, bool bossWave)
        {
            if (ally != null && enemy != null) return $"{ally.Data.displayName}  vs  {enemy.Data.displayName}";
            if (ally != null) return $"{ally.Data.displayName}  vs  empty lane";
            return $"Undefended lane  vs  {enemy.Data.displayName}";
        }

        private static UnitBase Alive(UnitBase u) => u != null && !u.IsDead ? u : null;

        private IEnumerator CameraHome()
        {
            if (_camera != null) yield return _camera.Home();
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
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

        private IEnumerator Attack(UnitBase attacker, UnitBase target, int lane, Faction side, System.Action<float> climate)
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
                    climate?.Invoke(_lanes.ReportAllyAttack(lane, false, attacker.TopPosition));
                }
                else
                {
                    climate?.Invoke(_lanes.ReportEnemyAttack(lane, false, attacker.TopPosition));
                }
                _ui.RefreshHud();
            });
        }

        /// <summary>The boss's turn: one cleave (target lane ± 1) or one slam (all 6 lanes).</summary>
        private IEnumerator BossPhase(BossUnit boss)
        {
            BossUnit.BossMove move = boss.PlanNext(_grid);
            _ui.ShowBossMove(move, boss);
            string what = move.IsSlam
                ? $"LANDSLIDE SLAM on all 6 lanes, {move.Damage} damage each"
                : $"CLEAVE on lanes {move.Lanes[0] + 1}–{move.Lanes[move.Lanes.Count - 1] + 1}, {move.Damage} damage each";
            _ui.ShowLaneCaption(boss.Data.displayName.ToUpperInvariant(), what, new Color(1f, 0.6f, 0.5f));
            if (_camera != null)
            {
                if (move.IsSlam) yield return _camera.Home();
                else yield return _camera.FocusLanes(move.Lanes[0], move.Lanes[move.Lanes.Count - 1]);
            }
            int hits = 0, exposed = 0, downed = 0;
            yield return boss.PerformRoutine(move.IsSlam, () =>
            {
                foreach (int lane in move.Lanes)
                {
                    UnitBase ally = _grid.GetAlly(lane);
                    Vector3 lanePos = IsoBoardLayout.SlotPosition(lane, false) + Vector3.up * 1.35f;
                    if (ally != null && !ally.IsDead)
                    {
                        ally.TakeDamage(move.Damage);
                        hits++;
                        _ui.ShowFloatingText(ally.TopPosition, $"-{move.Damage}", new Color(1f, 0.25f, 0.2f));
                        if (ally.IsDead) { downed++; _grid.Remove(ally); }
                    }
                    else
                    {
                        exposed++;
                        _lanes.ReportEnemyAttack(lane, false, lanePos);
                    }
                }
                _ui.RefreshHud();
            });
            string outcome = $"{hits} defender{(hits == 1 ? "" : "s")} hit" + (downed > 0 ? $", {downed} down" : "")
                + (exposed > 0 ? $" · {exposed} empty lane{(exposed == 1 ? "" : "s")}: +{exposed * GameManager.Instance.Level.heatPerUndefendedHit:0.00} °C" : " · no empty lanes, no heat");
            _ui.ShowLaneCaption(boss.Data.displayName.ToUpperInvariant(), outcome, exposed > 0 ? new Color(1f, 0.6f, 0.5f) : UIManager.Good);
            yield return Wait(0.6f);
            _ui.RefreshHud();
        }

    }
}
