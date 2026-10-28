using UnityEngine;

namespace ThermoTactics
{
    /// <summary>The outcome of one hit, before it is applied.</summary>
    public struct DamageResult
    {
        /// <summary>Damage the target loses (at least 1 when the attacker has any damage).</summary>
        public int Amount;
        /// <summary>The attacker's category counters the target's (×1.5).</summary>
        public bool Counter;
        /// <summary>Extra damage from the attacker's "+N vs X" trait.</summary>
        public int Bonus;
        /// <summary>Damage the target's armour blocked.</summary>
        public int Armour;
        /// <summary>The attacker ignored the target's armour (Pierce, Drill).</summary>
        public bool Pierced;
        /// <summary>Damage removed because the attacker is Weakened (Phase 3).</summary>
        public int Weakened;
        /// <summary>The target takes half damage from this attacker (Typhoon Tyrant vs Mangrove Guardian / Sea Turtle).</summary>
        public bool Halved;
    }

    /// <summary>
    /// Every damage rule in one place, so lane fights and boss attacks use the same maths:
    /// base damage → category counter ×1.5 → "+N vs X" trait (and board bonuses) → Weaken → armour (own trait + neighbours,
    /// unless pierced) → boss half-damage, at least 1.
    /// Counters: Forest > Extraction, Ocean > Waste, Renewable > Emissions (allies only).
    /// </summary>
    public static class DamageRules
    {
        public const float CounterMultiplier = 1.5f;

        /// <summary>
        /// One hit between two units on the board: the data rules plus what the board adds — synergy damage
        /// (already in attacker.Damage), Clear-cut Crew's +2 vs Forest, Dive's +2 on the first attack of a wave,
        /// Weaken, and armour from neighbours (Baseload Power, Garbage Patch, Understory, Canopy).
        /// </summary>
        public static DamageResult Calculate(UnitBase attacker, UnitBase target, int baseDamage = -1)
        {
            int extraBonus = 0;
            if (target != null && target.Data.category == UnitCategory.Forest) extraBonus += attacker.Synergy.VsForest;
            if (attacker.Data.trait == UnitTrait.Dive && !attacker.Status.attackedThisWave) extraBonus += 2;
            return Calculate(attacker.Data, target != null ? target.Data : null, baseDamage >= 0 ? baseDamage : attacker.Damage,
                             attacker.WeakenAmount, extraBonus, target != null ? target.ExtraArmour : 0);
        }

        public static DamageResult Calculate(UnitData attacker, UnitData target, int baseDamage, int weaken = 0, int extraBonus = 0, int extraArmour = 0)
        {
            var r = new DamageResult();
            if (attacker == null || baseDamage <= 0) return r;
            float dmg = baseDamage;

            if (target != null && attacker.faction == Faction.Ally && UnitCategoryInfo.Counters(attacker.category) == target.category)
            {
                r.Counter = true;
                dmg = RoundHalfUp(dmg * CounterMultiplier);
            }

            r.Bonus = (target != null ? BonusAgainst(attacker, target) : 0) + Mathf.Max(0, extraBonus);
            dmg += r.Bonus;

            r.Weakened = Mathf.Max(0, weaken);
            dmg -= r.Weakened;

            if (target != null)
            {
                int armour = ArmourOf(target) + Mathf.Max(0, extraArmour);
                if (armour > 0)
                {
                    if (Pierces(attacker)) r.Pierced = true;
                    else
                    {
                        int blocked = Mathf.Min(armour, Mathf.Max(0, (int)dmg - 1));
                        r.Armour = blocked;
                        dmg -= blocked;
                    }
                }
                if (attacker is BossData boss && boss.halfDamageTo != null && System.Array.IndexOf(boss.halfDamageTo, target) >= 0)
                {
                    r.Halved = true;
                    dmg = Mathf.Ceil(dmg / 2f);
                }
            }

            r.Amount = Mathf.Max(1, (int)dmg);
            return r;
        }

        /// <summary>The attacker's "+N vs X" trait bonus against this target (0 if it does not apply).</summary>
        public static int BonusAgainst(UnitData attacker, UnitData target)
        {
            int n = Mathf.RoundToInt(attacker.traitValue);
            switch (attacker.trait)
            {
                case UnitTrait.Gust: return target.isHover ? n : 0;
                case UnitTrait.Dive: return target.isHover ? n : 0;
                case UnitTrait.EarthCore: return target.category == UnitCategory.Extraction ? n : 0;
                case UnitTrait.Chainsaw: return target.category == UnitCategory.Forest ? n : 0;
                case UnitTrait.ClearCut: return target.category == UnitCategory.Forest ? n : 0;
                case UnitTrait.Poison: return target.category == UnitCategory.Ocean ? 2 : 0;
                case UnitTrait.Burn: return target.category == UnitCategory.Forest ? 3 : 0;
                default: return 0;
            }
        }

        /// <summary>"Damage taken −N" from the unit's own trait.</summary>
        public static int ArmourOf(UnitData unit)
        {
            switch (unit.trait)
            {
                case UnitTrait.ReefShield:
                case UnitTrait.BambooWall:
                case UnitTrait.Floodgate:
                case UnitTrait.Shell:
                case UnitTrait.Indestructible:
                    return Mathf.RoundToInt(unit.traitValue);
                case UnitTrait.MethaneLeak:
                    return 1;
                default:
                    return 0;
            }
        }

        public static bool Pierces(UnitData attacker) => attacker.trait == UnitTrait.Pierce || attacker.trait == UnitTrait.Drill;

        /// <summary>Night Eyes (Tarsier Scout) always attacks first.</summary>
        public static bool AlwaysFirst(UnitData unit) => unit != null && unit.trait == UnitTrait.NightEyes;

        /// <summary>
        /// True if the ally attacks before the enemy in a lane: higher Speed goes first, a tie goes to the ally,
        /// and Night Eyes always goes first.
        /// </summary>
        public static bool AllyStrikesFirst(UnitData ally, int allySpeed, UnitData enemy, int enemySpeed)
        {
            if (AlwaysFirst(ally)) return true;
            if (AlwaysFirst(enemy)) return false;
            return allySpeed >= enemySpeed;
        }

        private static float RoundHalfUp(float x) => Mathf.Floor(x + 0.5f);
    }
}
