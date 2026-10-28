using System.Collections.Generic;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>Owns the board state: which unit stands in each of the 12 slots (6 lanes × ally/enemy).</summary>
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private BoardSlot[] _allySlots = new BoardSlot[IsoBoardLayout.LaneCount];
        [SerializeField] private BoardSlot[] _enemySlots = new BoardSlot[IsoBoardLayout.LaneCount];

        private readonly UnitBase[] _allies = new UnitBase[IsoBoardLayout.LaneCount];
        private readonly UnitBase[] _enemies = new UnitBase[IsoBoardLayout.LaneCount];

        public int LaneCount => IsoBoardLayout.LaneCount;

        /// <summary>A pair synergy formed for the first time in this level (name, effect text, side).</summary>
        public event System.Action<string, string, Faction> SynergyFormed;
        private readonly HashSet<string> _announced = new HashSet<string>();

        /// <summary>The boss on the board, if any. It occupies every enemy slot.</summary>
        public BossUnit Boss { get; private set; }

        public IEnumerable<UnitBase> Allies { get { foreach (var a in _allies) if (a != null) yield return a; } }

        /// <summary>Each enemy once (the boss counts once even though it fills all lanes).</summary>
        public IEnumerable<UnitBase> Enemies
        {
            get
            {
                var seen = new HashSet<UnitBase>();
                foreach (var e in _enemies) if (e != null && seen.Add(e)) yield return e;
            }
        }

        public void SetSlots(BoardSlot[] allySlots, BoardSlot[] enemySlots)
        {
            _allySlots = allySlots;
            _enemySlots = enemySlots;
        }

        public UnitBase GetAlly(int lane) => Valid(lane) ? _allies[lane] : null;
        public UnitBase GetEnemy(int lane) => Valid(lane) ? _enemies[lane] : null;

        public bool IsAllySlotFree(int lane) => Valid(lane) && _allies[lane] == null;

        public void Place(UnitBase unit, int lane)
        {
            if (!Valid(lane) || unit == null) return;
            if (unit.Faction == Faction.Ally) _allies[lane] = unit; else _enemies[lane] = unit;
            RefreshSynergies();
        }

        /// <summary>
        /// Recomputes every unit's pair synergies and the Canopy aura (lane ± 1). Called after every deploy,
        /// defeat and wave spawn. Raises SynergyFormed the first time a pair appears in this level.
        /// </summary>
        public void RefreshSynergies()
        {
            RefreshSide(_allies, Faction.Ally);
            if (Boss == null) RefreshSide(_enemies, Faction.Enemy);
        }

        private void RefreshSide(UnitBase[] units, Faction side)
        {
            var data = new UnitData[units.Length];
            for (int i = 0; i < units.Length; i++)
                data[i] = units[i] != null && !units[i].IsDead ? units[i].Data : null;
            SynergyBonus[] bonus = SynergyRules.Compute(data, side);
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] == null) continue;
                units[i].SetSynergy(bonus[i]);
                foreach (string name in bonus[i].Names)
                    if (name != "Canopy" && _announced.Add(name)) SynergyFormed?.Invoke(name, SynergyRules.Describe(name), side);
            }
        }

        /// <summary>Allies in the lanes next to this lane (lane ± 1).</summary>
        public IEnumerable<UnitBase> NeighbourAllies(int lane)
        {
            for (int d = -1; d <= 1; d += 2)
            {
                UnitBase a = GetAlly(lane + d);
                if (a != null && !a.IsDead) yield return a;
            }
        }

        /// <summary>Puts the boss in all 6 enemy slots.</summary>
        public void PlaceBoss(BossUnit boss)
        {
            Boss = boss;
            for (int i = 0; i < LaneCount; i++) _enemies[i] = boss;
            RefreshSynergies();
        }

        /// <summary>Frees the slot immediately (the unit may still be playing its death fade).</summary>
        public void Remove(UnitBase unit)
        {
            if (unit == null) return;
            for (int i = 0; i < LaneCount; i++)
            {
                if (_allies[i] == unit) _allies[i] = null;
                if (_enemies[i] == unit) _enemies[i] = null;
            }
            if (unit == Boss) Boss = null;
            RefreshSynergies();
        }

        public int EnemyCount
        {
            get { int n = 0; foreach (var e in Enemies) n++; return n; }
        }

        public List<int> FreeEnemyLanes()
        {
            var lanes = new List<int>();
            for (int i = 0; i < LaneCount; i++) if (_enemies[i] == null) lanes.Add(i);
            return lanes;
        }

        public void SetDeployHighlights(bool show)
        {
            for (int i = 0; i < LaneCount; i++)
                if (_allySlots != null && i < _allySlots.Length && _allySlots[i] != null)
                    _allySlots[i].SetDeployable(show && _allies[i] == null);
        }

        /// <summary>Marks the ally slots of the given lanes with the red boss warning (null clears all).</summary>
        public void SetDangerLanes(IList<int> lanes)
        {
            for (int i = 0; i < LaneCount; i++)
                if (_allySlots != null && i < _allySlots.Length && _allySlots[i] != null)
                    _allySlots[i].SetDanger(lanes != null && lanes.Contains(i));
        }

        /// <summary>Destroys every unit on the board (used when a level restarts).</summary>
        public void ClearAll()
        {
            for (int i = 0; i < LaneCount; i++)
            {
                if (_allies[i] != null) Destroy(_allies[i].gameObject);
                if (_enemies[i] != null) Destroy(_enemies[i].gameObject);
                _allies[i] = null;
                _enemies[i] = null;
            }
            Boss = null;
            _announced.Clear();
            foreach (var leftover in FindObjectsByType<UnitBase>(FindObjectsSortMode.None))
                Destroy(leftover.gameObject);
        }

        private bool Valid(int lane) => lane >= 0 && lane < LaneCount;
    }
}
