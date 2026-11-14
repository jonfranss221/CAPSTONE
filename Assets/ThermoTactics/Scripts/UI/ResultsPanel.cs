using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// One table panel with two uses. Players see the LEADERBOARD: every registered player on this phone ranked by total
    /// stars (equal stars share a rank), with their own row highlighted. Admins also get the full RESULTS table: every saved
    /// win or loss with the player's code and the date.
    /// </summary>
    public class ResultsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _title;
        [SerializeField] private Text _headerA;
        [SerializeField] private Text _headerB;
        [SerializeField] private Text _headerC;
        [SerializeField] private RectTransform _content;
        [SerializeField] private GameObject _rowTemplate;
        [SerializeField] private Text _emptyText;
        [SerializeField] private Text _footer;
        [SerializeField] private Button _closeButton;

        private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color MeColor = new Color(0.886f, 0.682f, 0.173f, 0.32f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.36f);
        private static readonly Color Soft = new Color(0.8f, 0.85f, 0.9f);

        private readonly List<GameObject> _rows = new List<GameObject>();

        private bool _ready, _opening;

        private void Awake()
        {
            Init();
            if (!_opening) _root.SetActive(false);
        }

        /// <summary>Wires the buttons once. The panel is saved hidden in the scene, so Show calls this too.</summary>
        private void Init()
        {
            if (_ready) return;
            _ready = true;
            _closeButton.onClick.AddListener(Hide);
            _rowTemplate.SetActive(false);
        }

        /// <summary>The player leaderboard. currentName (any capitals) is highlighted.</summary>
        public void ShowLeaderboard(List<LeaderboardRow> rows, string currentName)
        {
            Begin("LEADERBOARD", "#   NAME", "PLAYER CODE", "TOTAL STARS");
            int mine = -1;
            foreach (LeaderboardRow r in rows)
            {
                bool me = !string.IsNullOrEmpty(currentName) && string.Equals(r.Name, currentName, System.StringComparison.OrdinalIgnoreCase);
                GameObject row = AddRow(me);
                Set(row, "Name", $"{r.Rank,-3} {r.Name}", me ? Gold : Color.white);
                Set(row, "Result", r.Code, Soft);
                Set(row, "Date", r.Stars == 1 ? "1 star" : $"{r.Stars} stars", Gold);
                if (me) mine = r.Rank;
            }
            string you = mine > 0 ? $"You are #{mine}. " : "";
            End(rows.Count, "No players yet. Clear a level to get on the board.",
                $"{you}{rows.Count} player(s) on this phone · stars from every stage, best run per level.");
        }

        /// <summary>The admin Results table: every saved result, with the player's code.</summary>
        public void ShowRecords(List<GameResult> records, ResultsDatabase db)
        {
            Begin("RESULTS (ADMIN)", "NAME · CODE", "RESULT", "DATE CREATED");
            var codes = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (PlayerRecord p in db.GetPlayers()) codes[p.Name] = p.Code;
            foreach (GameResult rec in records)
            {
                GameObject row = AddRow(false);
                bool won = rec.result == "WIN";
                string code = codes.TryGetValue(rec.player_name ?? "", out string c) ? c : "—";
                Set(row, "Name", $"{rec.player_name} · {code}", Color.white);
                Set(row, "Result", won ? "WIN" : "LOSS", won ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.45f, 0.4f));
                Set(row, "Date", rec.created_at, Soft);
            }
            End(records.Count, "No results yet. Finish a level to record one.", $"{records.Count} record(s) · saved in {db.FilePath}");
        }

        public void Hide() => _root.SetActive(false);

        private void Begin(string title, string a, string b, string c)
        {
            _opening = true;
            Init();
            foreach (var r in _rows) Destroy(r);
            _rows.Clear();
            if (_title != null) _title.text = title;
            if (_headerA != null) _headerA.text = a;
            if (_headerB != null) _headerB.text = b;
            if (_headerC != null) _headerC.text = c;
        }

        private GameObject AddRow(bool highlight)
        {
            GameObject row = Instantiate(_rowTemplate, _content);
            row.SetActive(true);
            var img = row.GetComponent<Image>();
            if (img != null) img.color = highlight ? MeColor : RowColor;
            _rows.Add(row);
            return row;
        }

        private void End(int count, string emptyText, string footer)
        {
            _emptyText.text = emptyText;
            _emptyText.gameObject.SetActive(count == 0);
            _footer.text = footer;
            _root.SetActive(true);
            _opening = false;
            _root.transform.SetAsLastSibling();
        }

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
