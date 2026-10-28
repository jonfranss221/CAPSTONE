using UnityEngine;

namespace ThermoTactics
{
    public enum Faction { Ally, Enemy }

    /// <summary>
    /// Read-only definition of one unit type (stats, art and animations).
    /// One asset per unit lives in Assets/ThermoTactics/GameData/Units.
    /// </summary>
    [CreateAssetMenu(menuName = "Thermo-Tactics/Unit Data", fileName = "UnitData")]
    public class UnitData : ScriptableObject
    {
        [Header("Identity")]
        public string unitId = "A00";
        public string displayName = "Unit";
        public Faction faction = Faction.Ally;
        [Tooltip("Elite variant (Level 5 on): tougher copy of a base enemy, drawn with a red tint.")]
        public bool isElite;

        [Header("Stats")]
        [Min(0)] public int opCost = 2;
        [Min(1)] public int maxHp = 10;
        [Min(0)] public int damage = 3;
        [Tooltip("1–10. In a lane, the faster unit attacks first; on a tie the ally goes first (used in combat from Phase 2).")]
        [Range(1, 10)] public int speed = 3;
        public UnitCategory category = UnitCategory.Forest;
        [Tooltip("Flies or floats (Wind Caller, Eagle Striker, Diesel Drone, Smog Wraith, Methane Cloud, Typhoon Tyrant). Some traits target hovering units.")]
        public bool isHover;

        [Header("Trait")]
        public UnitTrait trait = UnitTrait.None;
        [Tooltip("The trait's number, e.g. 2 for \"+2 damage vs Extraction\" or 0.08 for \"cools −0.08 °C\".")]
        public float traitValue;
        [Tooltip("One-line description shown in game.")]
        [TextArea(1, 3)] public string traitText = "";

        [Header("Store")]
        [Tooltip("Owned by every player from the start (the 5 original allies).")]
        public bool isStarter;
        public UnitTier tier = UnitTier.Common;
        [Tooltip("Price in Carbon Credits (0 for starters and enemies).")]
        [Min(0)] public int priceCredits;
        [Tooltip("Allies: the stage that must be unlocked before the unit appears in the store. Enemies: the first stage it appears in.")]
        [Min(1)] public int unlockStage = 1;

        [Header("Art")]
        public Sprite card;
        public Sprite idlePose;
        public RuntimeAnimatorController animator;
        [Tooltip("Multiplied with the sprite colour (elite variants use a red tint).")]
        public Color tint = Color.white;

        [Header("Animation timing (seconds)")]
        [Tooltip("Time from the start of the attack until the hit lands (frame 6 of 12 at 14 fps).")]
        public float hitDelay = 0.43f;
        public float attackDuration = 0.86f;

        protected virtual void OnValidate()
        {
            if (faction == Faction.Ally && opCost < 1) opCost = 1;
            if (faction == Faction.Enemy) { opCost = 0; priceCredits = 0; isStarter = false; }
            if (isStarter) priceCredits = 0;
            attackDuration = Mathf.Max(attackDuration, hitDelay);
        }
    }
}
