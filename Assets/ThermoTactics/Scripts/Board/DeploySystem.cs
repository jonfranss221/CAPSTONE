using System;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>Places ally units: checks the slot, asks OxygenPointSystem to spend, then spawns.</summary>
    public class DeploySystem : MonoBehaviour
    {
        public static DeploySystem Instance { get; private set; }

        public event Action<UnitData> SelectionChanged;
        public event Action<UnitBase> Deployed;

        [SerializeField] private GridManager _grid;
        [SerializeField] private OxygenPointSystem _oxygen;
        [SerializeField] private UnitFactory _factory;
        [SerializeField] private UIManager _ui;

        public UnitData Selected { get; private set; }

        private void Awake() => Instance = this;

        public void Select(UnitData unit)
        {
            Selected = unit;
            SelectionChanged?.Invoke(unit);
        }

        public void TryDeploy(int lane)
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.CanPlayerAct) return;

            if (Selected == null) { _ui.Announce("Pick a unit card first.", UIManager.Warning); return; }
            if (!_grid.IsAllySlotFree(lane)) { _ui.Announce("That slot is already taken.", UIManager.Warning); return; }
            if (!_oxygen.TrySpend(Selected.opCost))
            {
                _ui.Announce($"Not enough Oxygen: {Selected.displayName} needs {Selected.opCost} OP.", UIManager.Warning);
                return;
            }

            UnitBase unit = _factory.Spawn(Selected, lane, Selected.maxHp);
            _grid.Place(unit, lane);
            _grid.SetDeployHighlights(true);
            Deployed?.Invoke(unit);
        }
    }
}
