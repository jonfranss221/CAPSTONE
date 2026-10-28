namespace ThermoTactics
{
    /// <summary>
    /// What a unit stands for. Allies counter one enemy category (×1.5 damage, used from Phase 2):
    /// Forest beats Extraction, Ocean beats Waste, Renewable beats Emissions. Climate Disaster has no counter.
    /// </summary>
    public enum UnitCategory
    {
        // Allies
        Forest,
        Ocean,
        Renewable,
        // Enemies
        Extraction,
        Waste,
        Emissions,
        ClimateDisaster,
    }

    /// <summary>Store rarity of an unlockable ally (price range in Carbon Credits). Starters and enemies use Common.</summary>
    public enum UnitTier { Common, Rare, Epic, Legendary }

    /// <summary>
    /// Each unit's special rule from the roster (Docs/Thermo-Tactics_Roster.md). Phase 1 only stores it;
    /// the rules are applied in combat from Phase 2 (damage) and Phase 3 (status effects, auras, °C).
    /// </summary>
    public enum UnitTrait
    {
        None,
        // Allies
        BlueCarbon,     // A01 Mangrove Guardian: hitting an empty enemy slot cools more
        ReefShield,     // A02 Coral Defender: damage taken −N
        Gust,           // A06 Wind Caller: +N damage vs hovering enemies
        EarthCore,      // A08 Geothermal Sentinel: +N damage vs Extraction
        Pierce,         // A14 Reef Sentinel: ignores enemy armour
        BambooWall,     // A09 Bamboo Warden: damage taken −N
        NightEyes,      // A11 Tarsier Scout: always attacks first; dodges the first hit of each wave
        Harvest,        // A03 Rice Terrace Warden: +N Oxygen per turn while alive
        Regrowth,       // A04 Forest Spirit: heals itself N HP every turn
        Floodgate,      // A07 Hydro Guardian: damage taken −N; immune to Speed debuffs
        MeadowMend,     // A10 Seagrass Healer: heals allies in adjacent lanes N HP; cures Poison
        Shell,          // A12 Sea Turtle Guardian: damage taken −N
        Overcharge,     // A05 Solar Sentinel: +1 damage per turn survived (max N)
        Dive,           // A13 Eagle Striker: +N damage vs hovering enemies; first attack each wave +2
        Canopy,         // A15 Rainforest Ancient: adjacent allies take −1 damage; cools N °C every turn
        // Enemies
        Chainsaw,       // E09 Illegal Logger: +N damage vs Forest allies
        Slick,          // E07 Oil Slick: the ally it hits gets −N Speed next turn
        Greenhouse,     // E12 Methane Cloud: an empty-lane hit adds N °C instead of the level's amount
        Indestructible, // E05 Plastic Golem: damage taken −N
        Landslide,      // E10 Quarry Colossus: its hit also deals N to allies in adjacent lanes
        Haze,           // E03 Smog Wraith: the ally it hits is Weakened −N damage next turn
        Drill,          // E08 Mining Ravager: Pierce (ignores ally armour)
        ClearCut,       // E04 Deforestation Beast: +N damage vs Forest allies
        Soot,           // E01 Coal Brute: Weakens the ally it hits −N; +0.05 °C on every hit
        Swarm,          // E02 Diesel Drone: +N damage for each Diesel Drone in an adjacent lane
        Poison,         // E11 Toxic Runoff: N damage per turn for 2 turns; +2 damage vs Ocean allies
        Burn,           // E13 Incinerator Fiend: N damage per turn for 2 turns; +3 damage vs Forest allies
        MethaneLeak,    // E14 Landfill Leviathan: +N °C every 2 turns while alive; damage taken −1
        Smokestacks,    // E06 Factory Titan (boss): +N °C every turn while alive
        StormSurge,     // E15 Typhoon Tyrant (boss): hits 2 random lanes each turn, every lane every 3rd turn
    }

    /// <summary>How a boss picks the lanes it hits (BossUnit uses CleaveAndSlam today; the others are for later stages).</summary>
    public enum BossPattern
    {
        CleaveAndSlam,   // B01 Clearcut Behemoth: cleave the strongest ally's lane ± radius; slam all lanes every Nth turn
        Overdrive,       // E06 Factory Titan: hit one lane; every Nth turn Overdrive hits 3 lanes
        StormSurge,      // E15 Typhoon Tyrant: hit N random lanes each turn; every lane every Nth turn
    }

    public static class UnitCategoryInfo
    {
        /// <summary>The enemy category an ally category deals bonus damage to, or null.</summary>
        public static UnitCategory? Counters(UnitCategory c) => c switch
        {
            UnitCategory.Forest => UnitCategory.Extraction,
            UnitCategory.Ocean => UnitCategory.Waste,
            UnitCategory.Renewable => UnitCategory.Emissions,
            _ => null,
        };

        public static bool IsAllyCategory(UnitCategory c) => c <= UnitCategory.Renewable;

        public static string Label(UnitCategory c) => c == UnitCategory.ClimateDisaster ? "Climate Disaster" : c.ToString();
    }
}
