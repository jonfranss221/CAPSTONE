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
            if (faction == Faction.Enemy) opCost = 0;
            attackDuration = Mathf.Max(attackDuration, hitDelay);
        }
    }
}
