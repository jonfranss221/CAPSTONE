using System;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Places and rearranges ally units.
    /// Deploy: pick a unit card, then tap an empty ally slot (costs the unit's OP).
    /// Pick up an ally: tap it on the board (gold ring), then
    ///   • tap an empty slot to move it there. A unit placed THIS turn moves for free; one placed in an
    ///     earlier turn may move only while no OP has been spent this turn, and the move uses all of it;
    ///   • placed this turn only: tap another unit card to swap it (its OP is refunded first), or press
    ///     TAKE BACK to remove it with a full refund.
    /// Pressing END TURN locks every unit placed this turn (no more free moves, swaps or take-backs).
    /// </summary>
    public class DeploySystem : MonoBehaviour
    {
        public static DeploySystem Instance { get; private set; }

        public event Action<UnitData> SelectionChanged;
        public event Action<UnitBase> Deployed;
        /// <summary>A unit was moved, swapped or taken back (the game autosaves).</summary>
        public event Action Rearranged;
        /// <summary>The picked-up ally changed (null when nothing is picked up).</summary>
        public event Action<UnitBase> BoardSelectionChanged;

        [SerializeField] private GridManager _grid;
        [SerializeField] private OxygenPointSystem _oxygen;
        [SerializeField] private UnitFactory _factory;
        [SerializeField] private UIManager _ui;

        public UnitData Selected { get; private set; }

        /// <summary>The ally the player tapped on the board, waiting for a target slot, a card or TAKE BACK.</summary>
        public UnitBase BoardSelected { get; private set; }

        private void Awake() => Instance = this;

        public void Select(UnitData unit)
        {
            // A unit placed this turn is picked up and a different card is tapped: swap them.
            if (BoardSelected != null && unit != null && BoardSelected.DeployedThisTurn && unit != BoardSelected.Data)
            {
                TrySwap(BoardSelected, unit);
                return;
            }
            SetBoardSelected(null);
            Selected = unit;
            SelectionChanged?.Invoke(unit);
        }

        /// <summary>A tap on one of the player's six slots.</summary>
        public void OnAllySlotTapped(int lane)
        {
            UnitBase ally = _grid.GetAlly(lane);
            if (ally != null && !ally.IsDead)
            {
                if (!CanRearrange()) return;
                SetBoardSelected(BoardSelected == ally ? null : ally);   // tap again to put it down
                return;
            }
            if (BoardSelected != null) TryMove(BoardSelected, lane);
            else TryDeploy(lane);
        }

        public void TryDeploy(int lane)
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.CanPlayerAct) return;
            if (!TutorialController.AllowDeploy) { _ui.Announce("Follow the guide first: tap NEXT in the box.", UIManager.Warning); return; }

            if (Selected == null) { _ui.Announce("Pick a unit card first.", UIManager.Warning); return; }
            if (!_grid.IsAllySlotFree(lane)) { _ui.Announce("That slot is already taken.", UIManager.Warning); return; }
            if (!_oxygen.TrySpend(Selected.opCost))
            {
                _ui.Announce($"Not enough Oxygen: {Selected.displayName} needs {Selected.opCost} OP.", UIManager.Warning);
                return;
            }
            Spawn(Selected, lane);
        }

        /// <summary>Moves a picked-up ally to an empty slot. Free if it was placed this turn; otherwise only with
        /// this turn's Oxygen still unspent, and the move uses all of it.</summary>
        public void TryMove(UnitBase unit, int lane)
        {
            if (unit == null || !CanRearrange()) return;
            if (!_grid.IsAllySlotFree(lane)) { _ui.Announce("Pick an empty slot to move to.", UIManager.Warning); return; }

            string cost;
            if (unit.DeployedThisTurn) cost = "free, placed this turn";
            else
            {
                if (!_oxygen.IsUntouched)
                {
                    _ui.Announce($"You can move {unit.Data.displayName} only before spending any Oxygen this turn. Try again next turn.", UIManager.Warning);
                    return;
                }
                cost = $"used all {_oxygen.SpendAll()} OP";
            }

            if (!_grid.MoveAlly(unit, lane)) return;
            _ui.Announce($"{unit.Data.displayName} moved to Lane {lane + 1} ({cost}).", UIManager.Good);
            SetBoardSelected(null);
            _grid.SetDeployHighlights(true);
            Rearranged?.Invoke();
        }

        /// <summary>Removes the picked-up ally placed this turn and refunds its OP.</summary>
        public void TakeBack()
        {
            UnitBase unit = BoardSelected;
            if (unit == null || !unit.DeployedThisTurn || !CanRearrange()) return;
            int refund = unit.Data.opCost;
            _grid.Remove(unit);
            Destroy(unit.gameObject);
            _oxygen.Refund(refund);
            _ui.Announce($"{unit.Data.displayName} taken back (+{refund} OP).", UIManager.Good);
            SetBoardSelected(null);
            _grid.SetDeployHighlights(true);
            Rearranged?.Invoke();
        }

        /// <summary>END TURN: units placed this turn are locked in place.</summary>
        public void LockDeployments()
        {
            foreach (UnitBase a in _grid.Allies) a.DeployedThisTurn = false;
            SetBoardSelected(null);
        }

        private void TrySwap(UnitBase old, UnitData replacement)
        {
            if (!CanRearrange()) return;
            int lane = old.Lane;
            int refund = old.Data.opCost;
            if (_oxygen.Current + refund < replacement.opCost)
            {
                _ui.Announce($"Not enough Oxygen: {replacement.displayName} needs {replacement.opCost} OP " +
                             $"({_oxygen.Current} left + {refund} back from {old.Data.displayName}).", UIManager.Warning);
                return;
            }
            _grid.Remove(old);
            Destroy(old.gameObject);
            _oxygen.Refund(refund);
            _oxygen.TrySpend(replacement.opCost);
            SetBoardSelected(null);
            Selected = replacement;
            SelectionChanged?.Invoke(replacement);
            Spawn(replacement, lane);
            _ui.Announce($"{old.Data.displayName} swapped for {replacement.displayName} in Lane {lane + 1}.", UIManager.Good);
        }

        private void Spawn(UnitData data, int lane)
        {
            UnitBase unit = _factory.Spawn(data, lane, data.maxHp);
            unit.DeployedThisTurn = true;
            _grid.Place(unit, lane);
            _grid.SetDeployHighlights(true);
            Deployed?.Invoke(unit);
        }

        private bool CanRearrange()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.CanPlayerAct) return false;
            if (!TutorialController.AllowDeploy) { _ui.Announce("Follow the guide first: tap NEXT in the box.", UIManager.Warning); return false; }
            return true;
        }

        /// <summary>Puts down whatever is picked up (a level starts or restarts).</summary>
        public void ClearPickUp()
        {
            BoardSelected = null;
            _grid.SetSelectedLane(-1);
            BoardSelectionChanged?.Invoke(null);
        }

        private void SetBoardSelected(UnitBase unit)
        {
            if (ReferenceEquals(BoardSelected, unit)) return;
            BoardSelected = unit;
            _grid.SetSelectedLane(unit != null ? unit.Lane : -1);
            BoardSelectionChanged?.Invoke(unit);
        }
    }
}
