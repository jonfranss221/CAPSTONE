using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// A stage boss. It stands in all 6 enemy slots, so every ally lane attacks it.
    /// On its turn it either cleaves the lane of the player's strongest unit and its neighbours,
    /// or slams all 6 lanes (every 3rd boss turn; every 2nd once enraged). Below 40% HP it enrages.
    /// A boss hit on a lane with no ally heats the planet, like any undefended enemy attack.
    /// </summary>
    public class BossUnit : EnemyUnit
    {
        private static readonly int SlamTrigger = Animator.StringToHash("Slam");
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        private static readonly int EnragedBool = Animator.StringToHash("Enraged");

        public event Action<BossUnit> Enraged;

        public BossData Boss => Data as BossData;

        /// <summary>Boss turns already taken. The next turn is BossTurn + 1.</summary>
        public int BossTurn { get; private set; }
        public bool IsEnraged { get; private set; }

        public struct BossMove
        {
            public bool IsSlam;
            public List<int> Lanes;
            public int Damage;
        }

        // Slam or cleave is decided at the start of the player's turn, so the warning shown in the HUD
        // stays true even if the boss enrages during the ally phase. (The cleave target still follows the board.)
        private bool? _lockedSlam;

        public void RestoreTurn(int bossTurn) => BossTurn = Mathf.Max(0, bossTurn);

        /// <summary>Called at the start of every player turn: fixes whether the next boss attack is a slam.</summary>
        public void LockNextMove() => _lockedSlam = ScheduledSlam;

        private bool ScheduledSlam
        {
            get
            {
                int interval = Mathf.Max(1, SlamInterval);
                return (BossTurn + 1) % interval == 0;
            }
        }

        public int SlamInterval => Boss == null ? 3 : (IsEnraged ? Boss.enragedSlamEvery : Boss.slamEvery);

        /// <summary>Boss turns left until the slam (1 = the next enemy phase).</summary>
        public int TurnsUntilSlam
        {
            get
            {
                if (_lockedSlam == true) return 1;
                int interval = Mathf.Max(1, SlamInterval);
                int next = BossTurn + 1;
                int r = next % interval;
                int turns = r == 0 ? 1 : interval - r + 1;
                return _lockedSlam == false && turns == 1 ? 2 : turns;
            }
        }

        public int CleaveDamage => Boss == null ? Damage : (IsEnraged ? Boss.enragedCleaveDamage : Boss.damage);

        /// <summary>What the boss will do in the next enemy phase, given the board right now.</summary>
        public BossMove PlanNext(GridManager grid)
        {
            if (_lockedSlam ?? ScheduledSlam)
            {
                var all = new List<int>();
                for (int i = 0; i < grid.LaneCount; i++) all.Add(i);
                return new BossMove { IsSlam = true, Lanes = all, Damage = Boss != null ? Boss.slamDamage : Damage };
            }
            int target = StrongestAllyLane(grid);
            int radius = Boss != null ? Boss.cleaveRadius : 1;
            var lanes = new List<int>();
            for (int l = target - radius; l <= target + radius; l++)
                if (l >= 0 && l < grid.LaneCount) lanes.Add(l);
            return new BossMove { IsSlam = false, Lanes = lanes, Damage = CleaveDamage };
        }

        /// <summary>The lane of the ally with the most damage (ties: most HP, then the lower lane). No allies: lane 3.</summary>
        public static int StrongestAllyLane(GridManager grid)
        {
            int best = -1, bestDmg = -1, bestHp = -1;
            for (int l = 0; l < grid.LaneCount; l++)
            {
                UnitBase a = grid.GetAlly(l);
                if (a == null || a.IsDead) continue;
                if (a.Damage > bestDmg || (a.Damage == bestDmg && a.Hp > bestHp)) { best = l; bestDmg = a.Damage; bestHp = a.Hp; }
            }
            return best >= 0 ? best : 2;
        }

        /// <summary>Plays the cleave or slam animation and calls onHit on the damage frame.</summary>
        public IEnumerator PerformRoutine(bool slam, Action onHit)
        {
            if (Anim != null) Anim.SetTrigger(slam ? SlamTrigger : AttackTrigger);
            yield return new WaitForSeconds(Data.hitDelay);
            onHit?.Invoke();
            yield return new WaitForSeconds(Mathf.Max(0f, Data.attackDuration - Data.hitDelay));
            BossTurn++;
            _lockedSlam = null;
        }

        protected override void OnHpChanged()
        {
            if (IsEnraged || Boss == null || IsDead) return;
            if (Hp <= Mathf.FloorToInt(MaxHp * Boss.enrageAtFraction))
            {
                IsEnraged = true;
                if (Anim != null) Anim.SetBool(EnragedBool, true);
                if (Body != null) Body.color = new Color(1f, 0.86f, 0.82f, Body.color.a);
                Enraged?.Invoke(this);
            }
        }
    }
}
