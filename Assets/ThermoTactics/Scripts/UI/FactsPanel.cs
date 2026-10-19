using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// Climate Facts (the review list, "Climate Codex"): every fact of the stage grouped by quiz topic.
    /// Facts the player has met in a level can be reread here; the others show where they appear.
    /// </summary>
    public class FactsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _subtitle;
        [SerializeField] private RectTransform _content;
        [SerializeField] private GameObject _rowTemplate;
        [SerializeField] private Text _footer;
        [SerializeField] private Button _closeButton;

        private readonly List<GameObject> _rows = new List<GameObject>();

        private void Awake()
        {
            _closeButton.onClick.AddListener(Hide);
            _rowTemplate.SetActive(false);
            _root.SetActive(false);
        }

        public void Show(StageData stage, string player, ResultsDatabase db)
        {
            foreach (GameObject r in _rows) Destroy(r);
            _rows.Clear();
            HashSet<string> seen = db.GetSeenFacts(player);
            var facts = new List<PopupData>();
            if (stage.facts != null) foreach (PopupData f in stage.facts) if (f != null) facts.Add(f);
            // Grouped by topic, in quiz order; inside a topic, in the order they appear in the stage.
            facts.Sort((a, b) =>
            {
                int t = TopicOrder(stage.quiz, a.topicId).CompareTo(TopicOrder(stage.quiz, b.topicId));
                return t != 0 ? t : (a.levelNumber * 10 + (int)a.trigger * 5 + a.waveNumber).CompareTo(b.levelNumber * 10 + (int)b.trigger * 5 + b.waveNumber);
            });

            int read = 0;
            foreach (PopupData f in facts)
            {
                bool open = seen.Contains(f.factId);
                if (open) read++;
                GameObject row = Instantiate(_rowTemplate, _content);
                row.SetActive(true);
                string topic = PopupManager.TopicName(stage.quiz, f.topicId).ToUpperInvariant();
                Set(row, "Head", open
                    ? $"<color=#F3C95A>{topic}</color>  ·  <b>{f.title}</b>"
                    : $"<color=#8C8676>{topic}  ·  Locked</color>");
                Set(row, "Body", open ? f.body : $"<color=#8C8676>Appears in Level {f.levelNumber}" +
                                                 (f.trigger == PopupTrigger.AfterWave ? $" after Wave {f.waveNumber}." : ".") + "</color>");
                _rows.Add(row);
            }
            _subtitle.text = $"{stage.stageName} · {read} of {facts.Count} facts found. They cover every topic of the post-quiz.";
            _footer.text = SettingsStore.PopupsEnabled
                ? "New facts pop up during the levels."
                : "Climate fact pop-ups are off in Settings, so new facts will not appear.";
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Hide() => _root.SetActive(false);

        private static int TopicOrder(QuizData quiz, string topicId) => quiz != null ? quiz.TopicIndex(topicId) : 99;

        private static void Set(GameObject row, string child, string value)
        {
            Transform t = row.transform.Find(child);
            if (t != null) t.GetComponent<Text>().text = value;
        }
    }
}
