using UnityEngine;

namespace ThermoTactics
{
    /// <summary>One of the 5 stages: its 10 levels in order, and every unit used in it.</summary>
    [CreateAssetMenu(menuName = "Thermo-Tactics/Stage Data", fileName = "StageData")]
    public class StageData : ScriptableObject
    {
        [Min(1)] public int stageId = 1;
        public string stageName = "Baguio Forest";
        public string region = "Cordillera mountains · pine forest, fog, Kennon Road";
        public string threats = "illegal logging, mining, smog";
        public string difficulty = "Easy";
        public Sprite thumbnail;

        [Tooltip("Levels in play order; beating one unlocks the next.")]
        public LevelData[] levels = new LevelData[0];

        [Tooltip("Every unit that can appear in this stage. Used to rebuild the board from a saved game.")]
        public UnitData[] unitCatalog = new UnitData[0];

        public int LevelCount => levels != null ? levels.Length : 0;

        public LevelData GetLevel(int levelNumber)
        {
            int i = levelNumber - 1;
            return levels != null && i >= 0 && i < levels.Length ? levels[i] : null;
        }

        public UnitData FindUnit(string unitId)
        {
            if (unitCatalog == null || string.IsNullOrEmpty(unitId)) return null;
            foreach (UnitData u in unitCatalog)
                if (u != null && u.unitId == unitId) return u;
            return null;
        }
    }
}
