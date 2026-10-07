using System;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>Owns the Oxygen Point (OP) balance. Only this script changes OP.</summary>
    public class OxygenPointSystem : MonoBehaviour
    {
        public event Action Changed;

        public int Current { get; private set; }
        public int Allowance { get; private set; }
        public int Penalty { get; private set; }

        /// <summary>
        /// Starts a turn: OP is reset (unspent OP does not carry over) to the wave allowance
        /// minus the punishment for how long the wave has lasted. Never below 1.
        /// </summary>
        public void StartTurn(int waveAllowance, int penalty)
        {
            Penalty = Mathf.Max(0, penalty);
            Allowance = Mathf.Max(1, waveAllowance - Penalty);
            Current = Allowance;
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
