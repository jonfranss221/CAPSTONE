using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>The "Results" record panel: lists every saved result (name, win/loss, date created).</summary>
    public class ResultsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _content;
        [SerializeField] private GameObject _rowTemplate;
        [SerializeField] private Text _emptyText;
        [SerializeField] private Text _footer;
        [SerializeField] private Button _closeButton;

        private readonly List<GameObject> _rows = new List<GameObject>();

        private void Awake()
        {
            _closeButton.onClick.AddListener(Hide);
            _rowTemplate.SetActive(false);
            _root.SetActive(false);
        }

        public void Show(List<GameResult> records, string filePath)
        {
            foreach (var r in _rows) Destroy(r);
            _rows.Clear();

            foreach (GameResult rec in records)
            {
                GameObject row = Instantiate(_rowTemplate, _content);
                row.SetActive(true);
                bool won = rec.result == "WIN";
                Set(row, "Name", rec.player_name, Color.white);
                Set(row, "Result", won ? "WIN" : "LOSS", won ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.45f, 0.4f));
                Set(row, "Date", rec.created_at, new Color(0.8f, 0.85f, 0.9f));
                _rows.Add(row);
            }

            _emptyText.gameObject.SetActive(records.Count == 0);
            _footer.text = $"{records.Count} record(s) · saved in {filePath}";
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Hide() => _root.SetActive(false);

        private static void Set(GameObject row, string child, string value, Color color)
        {
            Transform t = row.transform.Find(child);
            if (t == null) return;
            var text = t.GetComponent<Text>();
            text.text = value;
            text.color = color;
        }
    }
}
