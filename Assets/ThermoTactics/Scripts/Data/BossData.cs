using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// A stage boss. It fills all 6 enemy slots, so every ally lane faces it.
    /// Cleave hits the targeted lane and its neighbours; the slam hits all 6 lanes.
    /// Each stage has its own boss designed for its environment (Stage 1: Clearcut Behemoth, Stage 2: Typhoon Tyrant).
    /// The Factory Titan (E06) is defined with its own pattern for a later stage.
    /// </summary>
    [CreateAssetMenu(menuName = "Thermo-Tactics/Boss Data", fileName = "BossData")]
    public class BossData : UnitData
    {
        [Header("Boss pattern")]
        [Tooltip("How the boss picks lanes. CleaveAndSlam: Stage 1. StormSurge: Stage 2 (a band of lanes that moves every turn). Overdrive is for a later stage.")]
        public BossPattern pattern = BossPattern.CleaveAndSlam;
        [Tooltip("Overdrive: lanes hit by the special attack. StormSurge: width of the storm band (+1 once enraged).")]
        [Range(1, 6)] public int specialLanes = 3;
        [Tooltip("Units that take half damage from this boss (Typhoon Tyrant: Mangrove Guardian and Sea Turtle Guardian).")]
        public UnitData[] halfDamageTo = new UnitData[0];

        [Header("Boss attacks")]
        [Tooltip("Cleave damage (uses 'damage' before enrage).")]
        [Min(0)] public int enragedCleaveDamage = 10;
        [Min(0)] public int slamDamage = 7;
        [Tooltip("The slam replaces the cleave every Nth boss turn.")]
        [Min(1)] public int slamEvery = 3;
        [Min(1)] public int enragedSlamEvery = 2;
        [Tooltip("Enrages when HP falls to this fraction of max HP or lower.")]
        [Range(0.05f, 0.95f)] public float enrageAtFraction = 0.4f;
        [Tooltip("Lanes hit on each side of the cleave target.")]
        [Min(0)] public int cleaveRadius = 1;

        [Header("Move names (shown in the HUD and captions)")]
        public string cleaveName = "CLEAVE";
        public string slamName = "LANDSLIDE SLAM";

        [Header("Forest shield (StormSurge)")]
        [Tooltip("Each living Forest ally cuts the slam (surge) damage by this much.")]
        [Min(0)] public int forestShieldPerAlly = 0;
        [Tooltip("The most the forest shield can cut.")]
        [Min(0)] public int forestShieldMax = 0;

        [Header("Placement")]
        [Tooltip("Board coordinates of the boss's feet (a along the lane, b across lanes).")]
        public Vector2 boardAnchor = new Vector2(4.5f, 3.0f);
        public float worldScale = 1.25f;
        [TextArea] public string description = "";

        protected override void OnValidate()
        {
            faction = Faction.Enemy;
            base.OnValidate();
        }
    }
}
