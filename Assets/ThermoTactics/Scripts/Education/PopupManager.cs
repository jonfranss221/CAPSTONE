using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// Shows the stage's climate facts as pop-ups during a level (at level start, or when a wave is cleared).
    /// Each fact reviews one quiz topic, so by Level 10 the player has read about every topic the post-quiz asks.
    /// A fact is shown once per player (facts_seen) and then stays readable in Climate Facts on the level path.
    /// The battle is frozen while a card is open. Settings ▸ "Climate fact pop-ups" turns them off.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        [SerializeField] private ResultsDatabase _database;
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _topic;
        [SerializeField] private Text _title;
        [SerializeField] private Text _body;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _counter;
        [SerializeField] private Button _okButton;

        /// <summary>True while a fact card is on screen (the player cannot act).</summary>
        public static bool IsOpen { get; private set; }

#if UNITY_EDITOR
        /// <summary>Editor play-tests: facts are marked seen without waiting for a tap.</summary>
        public static bool DevAutoDismiss;
#endif

        private bool _dismissed;

        private void Awake()
        {
            _okButton.onClick.AddListener(() => _dismissed = true);
            _root.SetActive(false);
            IsOpen = false;
        }

        private void OnDestroy() => IsOpen = false;

        /// <summary>Shows the unseen facts for this moment of the level, one after another, and waits until they are closed.</summary>
        public IEnumerator ShowFor(StageData stage, int level, PopupTrigger trigger, int wave)
        {
            if (!SettingsStore.PopupsEnabled || stage == null || stage.facts == null) yield break;
            GameManager gm = GameManager.Instance;
            string player = gm != null ? gm.PlayerName : GameSession.PlayerName;
            HashSet<string> seen = _database.GetSeenFacts(player);

            var due = new List<PopupData>();
            foreach (PopupData f in stage.facts)
                if (f != null && f.levelNumber == level && f.trigger == trigger &&
                    (trigger == PopupTrigger.LevelStart || f.waveNumber == wave) && !seen.Contains(f.factId))
                    due.Add(f);
            if (due.Count == 0) yield break;

            QuizData quiz = stage.quiz;
            int total = stage.facts.Length;
            for (int i = 0; i < due.Count; i++)
            {
                PopupData f = due[i];
                _topic.text = $"CLIMATE FACT · {TopicName(quiz, f.topicId).ToUpperInvariant()}";
                _title.text = f.title;
                _body.text = f.body;
                if (f.icon != null) _icon.sprite = f.icon;   // otherwise the card keeps its default icon
                _counter.text = $"Fact {seen.Count + 1} of {total} in {stage.stageName} · saved in Climate Facts";

                _dismissed = false;
                IsOpen = true;
                float speed = Time.timeScale;
                Time.timeScale = 0f;
                _root.SetActive(true);
                _root.transform.SetAsLastSibling();
#if UNITY_EDITOR
                if (DevAutoDismiss) _dismissed = true;
#endif
                while (!_dismissed) yield return null;
                _root.SetActive(false);
                IsOpen = false;
                Time.timeScale = speed;
                _database.MarkFactSeen(player, f.factId);
                seen.Add(f.factId);
            }
            if (gm != null) gm.ApplySpeed();
        }

        public static string TopicName(QuizData quiz, string topicId)
            => quiz != null ? quiz.TopicName(topicId) : string.IsNullOrEmpty(topicId) ? "Climate" : topicId;

#if UNITY_EDITOR
        /// <summary>Editor play-tests: closes the card on screen as if GOT IT was tapped.</summary>
        public void DevDismiss() => _dismissed = true;
#endif
    }
}
