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
        [Tooltip("Short name used in texts like \"Baguio Level 3\".")]
        public string shortName = "Baguio";

        [Header("Battle environment (swapped in by StageEnvironment)")]
        public Sprite battleBackground;
        [Tooltip("Blurred, darkened copy of the background that fills the sides on phones wider than 16:9.")]
        public Sprite sideFill;
        [Tooltip("Drifting weather overlay: fog in Baguio, sea spray on the coast.")]
        public Sprite overlayTile;
        [Tooltip("Overlay strength: 1 = Baguio fog. It still thins as the planet warms.")]
        [Range(0f, 1.5f)] public float overlayStrength = 1f;
        public Sprite slotMarker;
        [Tooltip("Deploy ring (blue), also tinted gold for a picked-up unit and yellow in high contrast. Empty = the scene's.")]
        public Sprite deployHighlight;
        [Tooltip("Colour the deploy ring is multiplied by (the ring files are white so the selected gold stays gold).")]
        public Color deployTint = Color.white;
        [Tooltip("Strength of the deploy ring while it is not hovered.")]
        [Range(0.3f, 1f)] public float deployAlpha = 0.65f;
        [Tooltip("Red ring on lanes the boss will hit next. Empty = the scene's.")]
        public Sprite dangerHighlight;
        public Color cameraBackground = new Color(0.055f, 0.082f, 0.067f);

        [Header("Next stage (shown when this stage is cleared)")]
        public string nextStageLabel = "Stage 2 · Sierra Madre Coast";
        [Tooltip("False while the next stage is not in the game yet (adds \"coming soon\").")]
        public bool nextStageReady = true;

        [Header("Menu theme")]
        [Tooltip("Level path art behind the 10 level nodes (same path for every stage).")]
        public Sprite levelPathArt;
        public StageTheme theme = StageTheme.Forest;

        [Tooltip("Levels in play order; beating one unlocks the next.")]
        public LevelData[] levels = new LevelData[0];

        [Tooltip("Every unit that can appear in this stage. Used to rebuild the board from a saved game.")]
        public UnitData[] unitCatalog = new UnitData[0];

        [Header("Education")]
        [Tooltip("Asked before the stage (pre-quiz) and again after its last level (post-quiz).")]
        public QuizData quiz;
        [Tooltip("Climate facts shown as pop-ups during the levels; they review the quiz topics.")]
        public PopupData[] facts = new PopupData[0];

        public int LevelCount => levels != null ? levels.Length : 0;
        public string ShortName => string.IsNullOrEmpty(shortName) ? stageName : shortName;

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
