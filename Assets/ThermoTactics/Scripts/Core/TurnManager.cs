using System.Collections;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Runs one turn after the player taps End Turn, lane by lane: in each lane the faster unit attacks first
    /// (a tie goes to the ally), then the other one if it is still standing → (wave cleared?) → the boss's cleave / slam
    /// → temperature check → next turn. All damage comes from DamageRules.
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

        private void OnEnable()
        {
            if (_grid != null) _grid.SynergyFormed += AnnounceSynergy;
        }

        private void OnDisable()
        {
            if (_grid != null) _grid.SynergyFormed -= AnnounceSynergy;
        }

        private void AnnounceSynergy(string name, string text, Faction side)
        {
            if (_ui == null) return;
            _ui.Announce(side == Faction.Ally ? $"Synergy: {text}" : $"Enemy synergy: {text}", side == Faction.Ally ? UIManager.Good : UIManager.Warning);
        }

        public void EndTurn()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.CanPlayerAct || !TutorialController.AllowEndTurn) return;
            StartCoroutine(ResolveTurn());
        }

        /// <summary>Gives the player the OP for the current turn of the current wave, then autosaves.</summary>
        public void BeginPlayerTurn(LevelData level)
        {
            WaveData wave = _waves.CurrentWave;
            int penalty = wave.ignorePenalty ? 0 : level.GetPenalty(_waves.TurnInWave);
            _oxygen.StartTurn(wave.opAllowance, penalty);
            GrantHarvest();
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

        /// <summary>Harvest: +1 Oxygen per living Rice Terrace Warden (+2 with Ridge to Reef), at most +2 in total.</summary>
        private void GrantHarvest()
        {
            int harvest = 0;
            UnitBase last = null;
            foreach (UnitBase a in _grid.Allies)
            {
                if (a.IsDead || a.Data.trait != UnitTrait.Harvest) continue;
                harvest += Mathf.Max(1, a.Synergy.Harvest);
                last = a;
            }
            harvest = Mathf.Min(2, harvest);
            if (harvest <= 0) return;
            _oxygen.AddBonus(harvest);
            _ui.ShowFloatingText(last.TopPosition, $"+{harvest} Oxygen", new Color(0.55f, 1f, 0.6f));
        }

        private void WarnIfSlamComing()
        {
            BossUnit boss = _grid.Boss;
            if (boss == null || !SettingsStore.BossWarning || boss.TurnsUntilSlam != 1) return;
            _ui.Announce($"Warning: {boss.Data.displayName} will slam ALL 6 lanes this turn! Fill every lane.", UIManager.Warning);
        }

        /// <summary>
        /// Resolves the turn one lane at a time, left to right. In each lane the camera zooms in and the faster unit
        /// attacks first (Speed; a tie goes to the ally). An ally hits the enemy, or the empty slot to cool the planet;
        /// an enemy hits the ally, or the undefended lane to heat the planet. A unit defeated by the first attack does
        /// not strike back. A lane with no units is shown briefly and skipped.
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

            // The boss acts once for all lanes, after every ally has hit it.
            if (!_waves.IsCurrentWaveCleared && _grid.Boss != null)
            {
                yield return BossPhase(_grid.Boss);
                _ui.HideLaneCaption();
                yield return Wait(0.3f);
            }

            // End of turn: Poison and Burn, heals, °C traits, then the effect timers count down.
            yield return EndOfTurn();
            yield return CameraHome();

            if (_temperature.HasReachedThreshold)
            {
                IsResolving = false;
                gm.EndLevel(false);
                yield break;
            }

            // Wave cleared? Then the next wave comes (or the level is won).
            if (_waves.IsCurrentWaveCleared)
            {
                yield return HandleWaveCleared(gm);
                IsResolving = false;
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

            // Who strikes first: the faster unit (a tie goes to the ally; Night Eyes always goes first).
            // In the boss wave only the allies attack here; the boss moves once after all lanes.
            bool allyFirst = ally == null || enemy == null || bossWave
                || DamageRules.AllyStrikesFirst(ally.Data, ally.Speed, enemy.Data, enemy.Speed);
            if (ally != null && enemy != null && !bossWave)
            {
                UnitBase lead = allyFirst ? ally : enemy, other = allyFirst ? enemy : ally;
                string why = DamageRules.AlwaysFirst(lead.Data) ? "Night Eyes"
                    : lead.Speed == other.Speed ? $"Speed {lead.Speed}, a tie goes to the defender"
                    : $"Speed {lead.Speed} vs {other.Speed}";
                _ui.ShowLaneCaption(title, $"{lead.Data.displayName} ({why}) strikes first", UIManager.Info);
                yield return Wait(0.35f);
            }

            UnitBase[] order = allyFirst ? new[] { ally, enemy } : new[] { enemy, ally };
            foreach (UnitBase attacker in order)
            {
                if (attacker == null || attacker.IsDead || _temperature.HasReachedThreshold) continue;
                if (bossWave && attacker.Faction == Faction.Enemy) continue;
                bool opening = result.Length == 0;
                if (!opening) { result.Append(" · "); yield return Wait(0.1f); }

                bool isAlly = attacker.Faction == Faction.Ally;
                UnitBase target = Alive(isAlly ? _grid.GetEnemy(lane) : _grid.GetAlly(lane));
                bool coolingUsed = _lanes.CooledThisTurn;
                var hit = new HitInfo();
                yield return Attack(attacker, target, lane, attacker.Faction, hit);
                climate += hit.Climate;

                if (target != null && hit.Dodged)
                {
                    result.Append($"{target.Data.displayName} dodges {attacker.Data.displayName}'s attack (Night Eyes)");
                }
                else if (target != null)
                {
                    result.Append($"{attacker.Data.displayName} {(opening ? "hits" : "hits back")}");
                    if (opening) result.Append($" {target.Data.displayName}");
                    result.Append($" for {hit.Dealt}{Explain(hit.Result)}");
                    if (target.IsDead)
                        result.Append(target is BossUnit ? ". The boss falls!" : isAlly ? $". {target.Data.displayName} defeated!" : $". {target.Data.displayName} is down!");
                }
                else if (isAlly)
                {
                    if (hit.Climate < 0f) result.Append($"{attacker.Data.displayName} attacks the empty lane: the air cools {hit.Climate:0.00} °C");
                    else if (!coolingUsed) result.Append($"{attacker.Data.displayName} attacks the empty lane (already at the {_temperature.Baseline:0.00} °C minimum)");
                    else result.Append($"{attacker.Data.displayName} attacks the empty lane (cooling counts once per turn)");
                }
                else result.Append($"{attacker.Data.displayName} attacks the undefended lane: +{hit.Climate:0.00} °C");
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
            if (gm.Popups != null) yield return gm.Popups.ShowFor(gm.Stage, gm.LevelNumber, PopupTrigger.AfterWave, _waves.WaveNumber);
            _waves.SpawnNextWave();
            foreach (UnitBase u in _grid.Allies) u.OnWaveStart();
            foreach (UnitBase u in _grid.Enemies) u.OnWaveStart();
            IsResolving = false;
            BeginPlayerTurn(gm.Level);
            if (_waves.IsBossWave && _grid.Boss != null)
                _ui.Announce($"FINAL WAVE: the {_grid.Boss.Data.displayName} blocks all 6 lanes! {_waves.CurrentWave.opAllowance} Oxygen each turn, no penalty.", UIManager.Warning);
            else
                _ui.Announce($"Wave {_waves.WaveNumber} of {_waves.TotalWaves} incoming! +{_waves.CurrentWave.opAllowance} Oxygen each turn.", UIManager.Info);
        }

        /// <summary>What one attack did (filled in on the frame the blow lands).</summary>
        private class HitInfo
        {
            public DamageResult Result;
            public int Dealt;
            public float Climate;
            public bool Dodged;
        }

        /// <summary>" (×1.5 counter, +2 trait, armour −1)" for the lane caption, or "".</summary>
        private static string Explain(DamageResult r)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (r.Counter) parts.Add("×1.5 counter");
            if (r.Bonus > 0) parts.Add($"+{r.Bonus} trait");
            if (r.Weakened > 0) parts.Add($"weakened −{r.Weakened}");
            if (r.Armour > 0) parts.Add($"armour −{r.Armour}");
            if (r.Pierced) parts.Add("pierces armour");
            if (r.Halved) parts.Add("half damage");
            return parts.Count > 0 ? $" ({string.Join(", ", parts)})" : "";
        }

        /// <summary>
        /// Applies one hit worked out by DamageRules and shows it: the damage in red, a gold "×1.5!" on a counter,
        /// a grey "−N" for armour. A Night Eyes dodge cancels the hit. Then the attacker's on-hit trait lands on the
        /// target (Slick, Haze, Soot, Poison, Burn) and Soot adds +0.05 °C.
        /// </summary>
        private void Hit(UnitBase attacker, UnitBase target, Vector3 at, HitInfo hit, int baseDamage = -1)
        {
            attacker.Status.attackedThisWave = true;
            if (target.Status.dodge)
            {
                target.Status.dodge = false;
                hit.Dodged = true;
                target.RefreshStatusIcons();
                _ui.ShowFloatingText(at, "Dodged!", new Color(0.95f, 0.95f, 0.95f));
                return;
            }

            DamageResult r = DamageRules.Calculate(attacker, target, baseDamage);
            int before = target.Hp;
            target.TakeDamage(r.Amount);
            hit.Result = r;
            hit.Dealt = Mathf.Min(before, r.Amount);
            _ui.ShowFloatingText(at, $"-{r.Amount}", new Color(1f, 0.25f, 0.2f));
            if (r.Counter) _ui.ShowFloatingText(at + Vector3.up * 0.45f, "×1.5!", new Color(1f, 0.82f, 0.25f));
            if (r.Armour > 0) _ui.ShowFloatingText(at + new Vector3(0.55f, 0.2f, 0f), $"−{r.Armour}", new Color(0.72f, 0.72f, 0.72f));

            UnitData a = attacker.Data;
            if (!target.IsDead)
            {
                switch (a.trait)
                {
                    case UnitTrait.Slick:
                        if (target.Data.trait != UnitTrait.Floodgate) target.Status.AddSlow(Mathf.RoundToInt(a.traitValue) + attacker.Synergy.SlickExtra);
                        break;
                    case UnitTrait.Haze:
                    case UnitTrait.Soot:
                        target.Status.AddWeaken(Mathf.RoundToInt(a.traitValue));
                        break;
                    case UnitTrait.Poison:
                        target.Status.AddPoison(Mathf.RoundToInt(a.traitValue), 2 + attacker.Synergy.PoisonTurns);
                        break;
                    case UnitTrait.Burn:
                        target.Status.AddBurn(Mathf.RoundToInt(a.traitValue), 2);
                        break;
                }
                target.RefreshStatusIcons();
            }
            if (a.trait == UnitTrait.Soot) hit.Climate += _lanes.AddClimate(0.05f, at + Vector3.up * 0.5f, "soot");
        }

        private IEnumerator Attack(UnitBase attacker, UnitBase target, int lane, Faction side, HitInfo hit)
        {
            bool hadTarget = target != null && !target.IsDead;
            yield return attacker.AttackRoutine(() =>
            {
                if (hadTarget && target != null && !target.IsDead)
                {
                    // A boss spans every lane: show each hit above the lane that dealt it.
                    Vector3 at = target is BossUnit
                        ? IsoBoardLayout.SlotPosition(lane, true) + Vector3.up * 1.6f
                        : target.TopPosition;
                    Hit(attacker, target, at, hit);
                    if (target.IsDead) _grid.Remove(target);
                }
                else if (side == Faction.Ally)
                {
                    attacker.Status.attackedThisWave = true;
                    hit.Climate += _lanes.ReportAllyAttack(lane, attacker, attacker.TopPosition);
                }
                else
                {
                    hit.Climate += _lanes.ReportEnemyAttack(lane, attacker, attacker.TopPosition);
                }

                // Landslide (Quarry Colossus): the blow also hits the allies in the lanes next to it.
                if (attacker.Data.trait == UnitTrait.Landslide && side == Faction.Enemy)
                {
                    int splash = Mathf.RoundToInt(attacker.Data.traitValue);
                    foreach (UnitBase n in new System.Collections.Generic.List<UnitBase>(_grid.NeighbourAllies(lane)))
                    {
                        n.TakeDamage(splash);
                        _ui.ShowFloatingText(n.TopPosition, $"-{splash} landslide", new Color(1f, 0.45f, 0.3f));
                        if (n.IsDead) _grid.Remove(n);
                    }
                }
                _ui.RefreshHud();
            });
        }

        /// <summary>
        /// After the lanes and the boss: Poison and Burn damage, heals (Regrowth, Meadow Mend, Blue Carbon Belt),
        /// °C traits (Canopy, Smokestacks, Methane Leak), then every effect timer counts down.
        /// </summary>
        private IEnumerator EndOfTurn()
        {
            var notes = new System.Collections.Generic.List<string>();
            var allies = new System.Collections.Generic.List<UnitBase>(_grid.Allies);
            var enemies = new System.Collections.Generic.List<UnitBase>(_grid.Enemies);
            var everyone = new System.Collections.Generic.List<UnitBase>(allies);
            everyone.AddRange(enemies);

            // 1. Poison and Burn.
            foreach (UnitBase u in everyone)
            {
                if (u == null || u.IsDead) continue;
                int dot = u.Status.DamageOverTime;
                if (dot <= 0) continue;
                string kind = u.Status.Poisoned && u.Status.Burning ? "poison + burn" : u.Status.Poisoned ? "poison" : "burn";
                u.TakeDamage(dot);
                _ui.ShowFloatingText(u.TopPosition, $"-{dot} {kind}", u.Status.Poisoned ? new Color(0.45f, 0.9f, 0.3f) : new Color(1f, 0.55f, 0.15f));
                notes.Add($"{u.Data.displayName} takes {dot} from {kind}" + (u.IsDead ? " and is down" : ""));
                if (u.IsDead) _grid.Remove(u);
            }

            // 2. Heals.
            foreach (UnitBase a in allies)
            {
                if (a == null || a.IsDead) continue;
                int healed = 0;
                if (a.Data.trait == UnitTrait.Regrowth) healed += a.Heal(Mathf.RoundToInt(a.Data.traitValue));
                if (a.Synergy.HealPerTurn > 0) healed += a.Heal(a.Synergy.HealPerTurn);
                if (healed > 0) _ui.ShowFloatingText(a.TopPosition + Vector3.up * 0.3f, $"+{healed}", new Color(0.55f, 1f, 0.6f));
                if (a.Data.trait == UnitTrait.MeadowMend)
                {
                    foreach (UnitBase n in _grid.NeighbourAllies(a.Lane))
                    {
                        bool cured = n.Status.Poisoned;
                        n.Status.CurePoison();
                        int h = n.Heal(Mathf.RoundToInt(a.Data.traitValue));
                        if (h > 0 || cured) _ui.ShowFloatingText(n.TopPosition + Vector3.up * 0.3f, cured ? $"+{h} cured" : $"+{h}", new Color(0.55f, 1f, 0.6f));
                    }
                    notes.Add($"{a.Data.displayName} heals its neighbours");
                }
            }

            // 3. Temperature traits.
            foreach (UnitBase a in allies)
                if (a != null && !a.IsDead && a.Data.trait == UnitTrait.Canopy)
                {
                    float d = _lanes.AddClimate(-a.Data.traitValue, a.TopPosition + Vector3.up * 0.5f, "canopy");
                    if (d < 0f) notes.Add($"{a.Data.displayName}'s canopy cools {d:0.00} °C");
                }
            foreach (UnitBase e in enemies)
            {
                if (e == null || e.IsDead) continue;
                bool leak = e.Data.trait == UnitTrait.MethaneLeak && e.Status.turnsAlive % 2 == 1;
                if (e.Data.trait == UnitTrait.Smokestacks || leak)
                {
                    float d = _lanes.AddClimate(e.Data.traitValue, e.TopPosition + Vector3.up * 0.5f, leak ? "methane" : "smoke");
                    notes.Add($"{e.Data.displayName} adds +{d:0.00} °C");
                }
            }

            // 4. Timers.
            foreach (UnitBase u in everyone)
            {
                if (u == null || u.IsDead) continue;
                u.Status.Tick();
                u.RefreshStatusIcons();
            }
            _grid.RefreshSynergies();
            _ui.RefreshHud();

            if (notes.Count > 0)
            {
                _ui.ShowLaneCaption("END OF TURN", string.Join(" · ", notes), UIManager.Info);
                yield return Wait(0.9f);
                _ui.HideLaneCaption();
            }
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
                        Hit(boss, ally, ally.TopPosition, new HitInfo(), move.Damage);
                        hits++;
                        if (ally.IsDead) { downed++; _grid.Remove(ally); }
                    }
                    else
                    {
                        exposed++;
                        _lanes.ReportEnemyAttack(lane, boss, lanePos);
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
