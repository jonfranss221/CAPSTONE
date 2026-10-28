using System.Collections.Generic;

namespace ThermoTactics
{
    /// <summary>Bonuses one unit gets from the units in the lanes next to it (lane ± 1).</summary>
    public class SynergyBonus
    {
        public int Damage, Speed, Armour;
        /// <summary>Extra damage against Forest allies (Clear-cut Crew).</summary>
        public int VsForest;
        /// <summary>Extra turns of Poison (Spill Zone).</summary>
        public int PoisonTurns;
        /// <summary>Extra Speed taken by Slick (Spill Zone).</summary>
        public int SlickExtra;
        /// <summary>Oxygen this Rice Terrace Warden gives each turn (1, or 2 with Ridge to Reef).</summary>
        public int Harvest;
        /// <summary>HP healed at the end of every turn (Blue Carbon Belt).</summary>
        public int HealPerTurn;
        /// <summary>Empty-slot cooling for Mangrove Guardian with Blue Carbon Belt.</summary>
        public bool BlueCarbonBelt;
        /// <summary>Names of the active pairs and auras, for the icon and the hint.</summary>
        public readonly List<string> Names = new List<string>();

        public bool Any => Names.Count > 0;
    }

    /// <summary>
    /// The pair synergies (two units in neighbouring lanes) and the Canopy aura. Pure rules on unit data,
    /// so they can be checked without a scene. GridManager recalculates them after every deploy, defeat and wave.
    /// Ally pairs are buffs; enemy pairs are debuffs for the player.
    /// </summary>
    public static class SynergyRules
    {
        public struct Pair
        {
            public string Name, A, Text;
            public string[] B;
            public Faction Side;
        }

        public static readonly Pair[] Pairs =
        {
            // Allies
            new Pair { Side = Faction.Ally, Name = "Blue Carbon Belt", A = "A01", B = new[] { "A10" },
                       Text = "Mangrove Guardian + Seagrass Healer: both heal 1 HP every turn; empty-slot cooling −0.10 °C" },
            new Pair { Side = Faction.Ally, Name = "Coral Triangle", A = "A02", B = new[] { "A14", "A12" },
                       Text = "Coral Defender + Reef Sentinel or Sea Turtle: +1 damage to both" },
            new Pair { Side = Faction.Ally, Name = "Hybrid Grid", A = "A05", B = new[] { "A06" },
                       Text = "Solar Sentinel + Wind Caller: +1 damage and +1 Speed to both" },
            new Pair { Side = Faction.Ally, Name = "Baseload Power", A = "A08", B = new[] { "A07" },
                       Text = "Geothermal Sentinel + Hydro Guardian: both take 1 less damage" },
            new Pair { Side = Faction.Ally, Name = "Ridge to Reef", A = "A03", B = new[] { "A07" },
                       Text = "Rice Terrace Warden + Hydro Guardian: Harvest gives +2 Oxygen instead of +1" },
            new Pair { Side = Faction.Ally, Name = "Forest Habitat", A = "A04", B = new[] { "A11", "A13" },
                       Text = "Forest Spirit + Tarsier Scout or Eagle Striker: the animal gets +1 damage and +1 Speed" },
            new Pair { Side = Faction.Ally, Name = "Understory", A = "A15", B = new[] { "A09" },
                       Text = "Rainforest Ancient + Bamboo Warden: Bamboo Warden takes 1 more less damage (2 in total)" },
            // Enemies
            new Pair { Side = Faction.Enemy, Name = "Fossil Chain", A = "E01", B = new[] { "E06", "E13" },
                       Text = "Coal Brute + Factory Titan or Incinerator Fiend: +1 damage to both" },
            new Pair { Side = Faction.Enemy, Name = "Drone Swarm", A = "E02", B = new[] { "E02" },
                       Text = "Diesel Drone + Diesel Drone: +1 damage for each neighbouring drone" },
            new Pair { Side = Faction.Enemy, Name = "Clear-cut Crew", A = "E09", B = new[] { "E04" },
                       Text = "Illegal Logger + Deforestation Beast: +2 more damage vs Forest allies" },
            new Pair { Side = Faction.Enemy, Name = "Spill Zone", A = "E07", B = new[] { "E11" },
                       Text = "Oil Slick + Toxic Runoff: Poison lasts 1 more turn; Slick takes −3 Speed" },
            new Pair { Side = Faction.Enemy, Name = "Garbage Patch", A = "E05", B = new[] { "E14" },
                       Text = "Plastic Golem + Landfill Leviathan: both take 1 more less damage" },
        };

        /// <summary>The roster id without an elite prefix (EL09 → E09), so elites join their base unit's pairs.</summary>
        public static string BaseId(UnitData u)
        {
            if (u == null) return null;
            return u.unitId.StartsWith("EL") ? "E" + u.unitId.Substring(2) : u.unitId;
        }

        /// <summary>
        /// Bonuses for every lane of one side. lanes[i] is the unit in lane i (null = empty).
        /// Returns one SynergyBonus per lane (empty bonus for empty lanes).
        /// </summary>
        public static SynergyBonus[] Compute(UnitData[] lanes, Faction side)
        {
            int n = lanes.Length;
            var result = new SynergyBonus[n];
            for (int i = 0; i < n; i++) result[i] = new SynergyBonus();

            for (int i = 0; i < n; i++)
            {
                UnitData u = lanes[i];
                if (u == null) continue;
                SynergyBonus b = result[i];
                string id = BaseId(u);

                if (u.trait == UnitTrait.Harvest) b.Harvest = 1;

                for (int d = -1; d <= 1; d += 2)
                {
                    int j = i + d;
                    if (j < 0 || j >= n || lanes[j] == null) continue;
                    string other = BaseId(lanes[j]);
                    foreach (Pair p in Pairs)
                    {
                        if (p.Side != side) continue;
                        bool iAmA = id == p.A && System.Array.IndexOf(p.B, other) >= 0;
                        bool iAmB = other == p.A && System.Array.IndexOf(p.B, id) >= 0;
                        if (!iAmA && !iAmB) continue;
                        // A pair counts once per unit (a Coral Defender between two Reef Sentinels still gets +1);
                        // Drone Swarm counts every neighbouring drone.
                        if (p.Name != "Drone Swarm" && b.Names.Contains(p.Name)) continue;
                        if (Apply(p.Name, b, u, iAmA, lanes[j]) && !b.Names.Contains(p.Name)) b.Names.Add(p.Name);
                    }
                    // Canopy: allies next to a Rainforest Ancient take 1 less damage.
                    if (side == Faction.Ally && lanes[j].trait == UnitTrait.Canopy)
                    {
                        b.Armour += 1;
                        if (!b.Names.Contains("Canopy")) b.Names.Add("Canopy");
                    }
                }
            }
            return result;
        }

        /// <summary>Adds one pair's effect to the unit's bonus. Returns false if the pair gives this unit nothing.</summary>
        private static bool Apply(string name, SynergyBonus b, UnitData me, bool iAmA, UnitData partner)
        {
            switch (name)
            {
                case "Blue Carbon Belt":
                    b.HealPerTurn = 1;
                    if (me.trait == UnitTrait.BlueCarbon) b.BlueCarbonBelt = true;
                    return true;
                case "Coral Triangle":
                case "Fossil Chain":
                    b.Damage += 1; return true;
                case "Hybrid Grid":
                    b.Damage += 1; b.Speed += 1; return true;
                case "Baseload Power":
                case "Garbage Patch":
                    b.Armour += 1; return true;
                case "Ridge to Reef":
                    if (me.trait == UnitTrait.Harvest) { b.Harvest = 2; return true; }
                    return true;
                case "Forest Habitat":
                    if (!iAmA) { b.Damage += 1; b.Speed += 1; }   // the animal (Tarsier / Eagle) gets the bonus
                    return true;
                case "Understory":
                    if (me.trait == UnitTrait.BambooWall) b.Armour += 1;
                    return true;
                case "Drone Swarm":
                    b.Damage += 1; return true;                    // once per neighbouring drone
                case "Clear-cut Crew":
                    b.VsForest += 2; return true;
                case "Spill Zone":
                    if (me.trait == UnitTrait.Poison) b.PoisonTurns += 1;
                    if (me.trait == UnitTrait.Slick) b.SlickExtra += 1;
                    return true;
            }
            return false;
        }

        public static string Describe(string name)
        {
            if (name == "Canopy") return "Canopy: next to a Rainforest Ancient, takes 1 less damage";
            foreach (Pair p in Pairs) if (p.Name == name) return p.Text;
            return name;
        }
    }
}
