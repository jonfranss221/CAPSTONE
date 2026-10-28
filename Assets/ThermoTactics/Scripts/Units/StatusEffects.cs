using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// The temporary effects on one unit and the counters some traits need.
    /// Poison and Burn deal damage at the end of each turn for their remaining turns.
    /// Weaken and Slow come from a hit and last for the unit's next turn: they start "fresh" and only count from
    /// the following turn, so a unit is never weakened for the counter-attack in the same lane.
    /// Dodge (Night Eyes) cancels the next hit. Saved with the unit, so Continue restores them.
    /// </summary>
    [System.Serializable]
    public class StatusEffects
    {
        public int poisonDamage, poisonTurns;
        public int burnDamage, burnTurns;
        public int weaken, weakenTurns; public bool weakenFresh;
        public int slow, slowTurns; public bool slowFresh;
        public bool dodge;
        /// <summary>Turns this unit has survived (Overcharge, Methane Leak).</summary>
        public int turnsAlive;
        /// <summary>Has attacked in the current wave (Dive's first-attack bonus).</summary>
        public bool attackedThisWave;

        public bool Poisoned => poisonTurns > 0;
        public bool Burning => burnTurns > 0;
        public int ActiveWeaken => weakenTurns > 0 && !weakenFresh ? weaken : 0;
        public int ActiveSlow => slowTurns > 0 && !slowFresh ? slow : 0;
        public bool Weakened => weakenTurns > 0;
        public bool Slowed => slowTurns > 0;

        public void AddPoison(int damage, int turns)
        {
            poisonDamage = Mathf.Max(poisonDamage, damage);
            poisonTurns = Mathf.Max(poisonTurns, turns);
        }

        public void AddBurn(int damage, int turns)
        {
            burnDamage = Mathf.Max(burnDamage, damage);
            burnTurns = Mathf.Max(burnTurns, turns);
        }

        public void AddWeaken(int amount)
        {
            weaken = Mathf.Max(weaken, amount);
            weakenTurns = 1;
            weakenFresh = true;
        }

        public void AddSlow(int amount)
        {
            slow = Mathf.Max(slow, amount);
            slowTurns = 1;
            slowFresh = true;
        }

        public void CurePoison() { poisonDamage = 0; poisonTurns = 0; }

        /// <summary>Damage from Poison and Burn this end of turn (before the timers count down).</summary>
        public int DamageOverTime => (Poisoned ? poisonDamage : 0) + (Burning ? burnDamage : 0);

        /// <summary>End of turn: timers count down; fresh effects become active for the next turn.</summary>
        public void Tick()
        {
            if (poisonTurns > 0 && --poisonTurns == 0) poisonDamage = 0;
            if (burnTurns > 0 && --burnTurns == 0) burnDamage = 0;
            if (weakenFresh) weakenFresh = false; else if (weakenTurns > 0 && --weakenTurns == 0) weaken = 0;
            if (slowFresh) slowFresh = false; else if (slowTurns > 0 && --slowTurns == 0) slow = 0;
            turnsAlive++;
        }

        public StatusEffects Copy() => (StatusEffects)MemberwiseClone();
    }
}
