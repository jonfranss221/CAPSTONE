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

        [Header("Stats")]
        [Min(0)] public int opCost = 2;
        [Min(1)] public int maxHp = 10;
        [Min(0)] public int damage = 3;

        [Header("Art")]
        public Sprite card;
        public Sprite idlePose;
        public RuntimeAnimatorController animator;

        [Header("Animation timing (seconds)")]
        [Tooltip("Time from the start of the attack until the hit lands (frame 6 of 12 at 14 fps).")]
        public float hitDelay = 0.43f;
        public float attackDuration = 0.86f;

        private void OnValidate()
        {
            if (faction == Faction.Ally && opCost < 1) opCost = 1;
            if (faction == Faction.Enemy) opCost = 0;
            attackDuration = Mathf.Max(attackDuration, hitDelay);
        }
    }
}
