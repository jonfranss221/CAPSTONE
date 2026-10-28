using System;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>Owns the Oxygen Point (OP) balance. Only this script changes OP.</summary>
    public class OxygenPointSystem : MonoBehaviour
    {
        public event Action Changed;

        [Tooltip("Oxygen a turn can never drop below. Keep it at least the cheapest unit's cost (2) so the player can always deploy.")]
        [SerializeField, Min(1)] private int _minimumPerTurn = 2;

        public int Current { get; private set; }
        public int Allowance { get; private set; }
        public int Penalty { get; private set; }

        /// <summary>
        /// Starts a turn: OP is reset (unspent OP does not carry over) to the wave allowance
        /// minus the punishment for how long the wave has lasted. Never below the minimum (2).
        /// </summary>
        public void StartTurn(int waveAllowance, int penalty)
        {
            Penalty = Mathf.Max(0, penalty);
            Allowance = Mathf.Max(_minimumPerTurn, waveAllowance - Penalty);
            Current = Allowance;
            Changed?.Invoke();
        }

        /// <summary>Puts back the balance of a saved game (no new allowance is granted).</summary>
        public void Restore(int current, int allowance, int penalty)
        {
            Allowance = Mathf.Max(0, allowance);
            Penalty = Mathf.Max(0, penalty);
            Current = Mathf.Clamp(current, 0, Mathf.Max(Allowance, current));
            Changed?.Invoke();
        }

        /// <summary>Extra Oxygen this turn (Harvest from Rice Terrace Wardens). Raises the allowance too, so the HUD reads e.g. 7 / 7.</summary>
        public void AddBonus(int amount)
        {
            if (amount <= 0) return;
            Allowance += amount;
            Current += amount;
            Changed?.Invoke();
        }

        public bool CanAfford(int cost) => Current >= cost;

        public bool TrySpend(int cost)
        {
            if (cost < 0 || !CanAfford(cost)) return false;
            Current -= cost;
            Changed?.Invoke();
            return true;
        }
    }
}
