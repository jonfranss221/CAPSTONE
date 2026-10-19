using UnityEngine;

namespace ThermoTactics
{
    public enum PopupTrigger { LevelStart, AfterWave }

    /// <summary>
    /// One climate fact shown as a pop-up during a level. Its topic matches a quiz question, so the facts
    /// a player reads during the stage review what the post-quiz asks. Each fact is shown once per player
    /// (facts_seen) and stays readable in the Climate Facts list.
    /// </summary>
    [CreateAssetMenu(menuName = "Thermo-Tactics/Popup Data", fileName = "PopupData")]
    public class PopupData : ScriptableObject
    {
        public string factId = "F01";
        [Min(1)] public int stageId = 1;
        [Tooltip("Matches QuizQuestion.topicId.")]
        public string topicId = "";
        public string title = "";
        [TextArea(3, 6)] public string body = "";
        public Sprite icon;

        [Header("When it appears")]
        [Range(1, 10)] public int levelNumber = 1;
        public PopupTrigger trigger = PopupTrigger.LevelStart;
        [Tooltip("AfterWave only: shown when this wave is cleared (before the next one).")]
        [Min(1)] public int waveNumber = 1;
    }
}
