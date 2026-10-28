using System.Collections.Generic;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Which allies a player can bring into a level (the starters plus every ally bought in the store) and the squad
    /// of up to 5 they picked. The last squad is kept per player in PlayerPrefs (tt.squad.&lt;name&gt;) so the squad
    /// screen opens with it; a saved game stores its own squad in saved_session.
    /// </summary>
    public static class PlayerSquad
    {
        public const int MaxSize = LevelData.MaxRosterTypes;
        private const string Prefix = "tt.squad.";

        /// <summary>Every ally of the stage catalog in roster order (A01–A15).</summary>
        public static List<UnitData> AllAllies(StageData stage)
        {
            var list = new List<UnitData>();
            if (stage == null || stage.unitCatalog == null) return list;
            foreach (UnitData u in stage.unitCatalog)
                if (u != null && u.faction == Faction.Ally && !list.Contains(u)) list.Add(u);
            list.Sort((a, b) => string.CompareOrdinal(a.unitId, b.unitId));
            return list;
        }

        public static bool Owns(UnitData u, HashSet<string> owned) => u != null && (u.isStarter || (owned != null && owned.Contains(u.unitId)));

        /// <summary>Allies this player can pick: the starters and the ones they bought.</summary>
        public static List<UnitData> Available(StageData stage, HashSet<string> owned)
        {
            var list = new List<UnitData>();
            foreach (UnitData u in AllAllies(stage)) if (Owns(u, owned)) list.Add(u);
            return list;
        }

        /// <summary>The level's own roster (the 5 starters in Baguio): used for new players and the Level 1 tutorial.</summary>
        public static UnitData[] Default(LevelData level) => level != null && level.allyRoster != null ? (UnitData[])level.allyRoster.Clone() : new UnitData[0];

        /// <summary>"A01,A06,A09" ↔ units (unknown or unowned ids are dropped).</summary>
        public static string Encode(IList<UnitData> squad)
        {
            var ids = new List<string>();
            if (squad != null) foreach (UnitData u in squad) if (u != null) ids.Add(u.unitId);
            return string.Join(",", ids);
        }

        public static UnitData[] Decode(string ids, StageData stage, HashSet<string> owned = null)
        {
            var list = new List<UnitData>();
            if (string.IsNullOrEmpty(ids) || stage == null) return list.ToArray();
            foreach (string id in ids.Split(','))
            {
                UnitData u = stage.FindUnit(id.Trim());
                if (u == null || u.faction != Faction.Ally || list.Contains(u)) continue;
                if (owned != null && !Owns(u, owned)) continue;
                list.Add(u);
                if (list.Count >= MaxSize) break;
            }
            return list.ToArray();
        }

        private static string Key(string player) => Prefix + (player ?? "").Trim().ToLowerInvariant();

        /// <summary>The player's last squad, or the level's default when there is none (or it is no longer valid).</summary>
        public static UnitData[] Load(string player, StageData stage, HashSet<string> owned, LevelData level)
        {
            UnitData[] saved = Decode(PlayerPrefs.GetString(Key(player), ""), stage, owned);
            return saved.Length > 0 ? saved : Default(level);
        }

        public static void Save(string player, IList<UnitData> squad)
        {
            PlayerPrefs.SetString(Key(player), Encode(squad));
            PlayerPrefs.Save();
        }

        public static void Clear(string player)
        {
            PlayerPrefs.DeleteKey(Key(player));
            PlayerPrefs.Save();
        }
    }
}
