using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>Owns every screen: login, HUD, deploy bar, end screen and the Results panel.</summary>
    public class UIManager : MonoBehaviour
    {
        public static readonly Color Info = new Color(0.85f, 0.95f, 1f);
        public static readonly Color Good = new Color(0.55f, 1f, 0.6f);
        public static readonly Color Warning = new Color(1f, 0.75f, 0.3f);

        [Header("Login")]
        [SerializeField] private GameObject _loginPanel;
        [SerializeField] private InputField _nameInput;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _loginResultsButton;
        [SerializeField] private Text _loginMessage;

        [Header("HUD")]
        [SerializeField] private GameObject _hud;
        [SerializeField] private Image _temperatureFill;
        [SerializeField] private Image _temperatureBack;
        [SerializeField] private Text _temperatureValue;
        [SerializeField] private Text _oxygenValue;
        [SerializeField] private Text _waveValue;
        [SerializeField] private Text _playerValue;
        [SerializeField] private Button _endTurnButton;
        [SerializeField] private DeployCard[] _cards = new DeployCard[0];
        [SerializeField] private Text _announcement;
        [SerializeField] private RectTransform _floatingLayer;

        [Header("End screen")]
        [SerializeField] private GameObject _endPanel;
        [SerializeField] private Text _endTitle;
        [SerializeField] private Text _endBody;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _endResultsButton;
        [SerializeField] private Button _menuButton;

        [Header("Results")]
        [SerializeField] private ResultsPanel _results;

        [Header("Systems")]
        [SerializeField] private TemperatureSystem _temperature;
        [SerializeField] private OxygenPointSystem _oxygen;
        [SerializeField] private WaveManager _waves;
        [SerializeField] private DeploySystem _deploy;
        [SerializeField] private TurnManager _turns;
        [SerializeField] private ResultsDatabase _database;
        [SerializeField] private Font _font;

        private Coroutine _announceRoutine;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            _nameInput.characterLimit = ResultsDatabase.MaxNameLength;
            _startButton.onClick.AddListener(OnStartClicked);
            _loginResultsButton.onClick.AddListener(OpenResults);
            _endResultsButton.onClick.AddListener(OpenResults);
            _endTurnButton.onClick.AddListener(() => _turns.EndTurn());
            _playAgainButton.onClick.AddListener(() => GameManager.Instance.Restart());
            _menuButton.onClick.AddListener(() => GameManager.Instance.BackToMenu());

            _temperature.Changed += _ => RefreshHud();
            _oxygen.Changed += RefreshHud;
            _deploy.SelectionChanged += _ => RefreshHud();
            _deploy.Deployed += _ => RefreshHud();
        }

        // ---------------- screens ----------------

        public void ShowLogin()
        {
            _loginPanel.SetActive(true);
            _hud.SetActive(false);
            _endPanel.SetActive(false);
            _results.Hide();
            _loginMessage.text = "";
            if (!string.IsNullOrEmpty(GameManager.Instance.PlayerName)) _nameInput.text = GameManager.Instance.PlayerName;
        }

        public void ShowHud()
        {
            _loginPanel.SetActive(false);
            _endPanel.SetActive(false);
            _results.Hide();
            _hud.SetActive(true);
            UnitData[] roster = GameManager.Instance.Level.allyRoster;
            for (int i = 0; i < _cards.Length; i++)
                _cards[i].Setup(i < roster.Length ? roster[i] : null, unit => _deploy.Select(unit));
            _playerValue.text = GameManager.Instance.PlayerName;
            RefreshHud();
        }

        public void ShowEnd(bool won, GameResult record, float threshold)
        {
            RefreshHud();
            _endPanel.SetActive(true);
            _endPanel.transform.SetAsLastSibling();
            _endTitle.text = won ? "VICTORY" : "DEFEAT";
            _endTitle.color = won ? Good : new Color(1f, 0.4f, 0.35f);
            _endBody.text = won
                ? $"Baguio is safe, {record.player_name}!\nAll {record.waves_cleared} waves cleared and the temperature stayed at {record.final_temperature:0.00}°C, below the {threshold:0.0}°C limit."
                : $"The temperature reached {record.final_temperature:0.00}°C, crossing the {threshold:0.0}°C limit.\nWaves cleared: {record.waves_cleared} of {_waves.TotalWaves}.";
            _endBody.text += $"\n\nResult saved · {record.created_at}";
        }

        public void OpenResults() => _results.Show(_database.GetAll(), _database.FilePath);

        private void OnStartClicked()
        {
            string name = ResultsDatabase.Sanitize(_nameInput.text);
            if (name.Length == 0)
            {
                _loginMessage.text = "Please enter a username.";
                return;
            }
            if (name.Length < 2)
            {
                _loginMessage.text = "Use at least 2 characters.";
                return;
            }
            GameManager.Instance.StartLevel(name);
        }

        // ---------------- HUD ----------------

        public void RefreshHud()
        {
            if (_temperature == null || _waves.CurrentWave == null) return;

            float n = _temperature.Normalized;
            _temperatureFill.fillAmount = Mathf.Max(0.02f, n);
            _temperatureFill.color = n < 0.5f
                ? Color.Lerp(new Color(0.3f, 0.8f, 0.45f), new Color(1f, 0.8f, 0.25f), n * 2f)
                : Color.Lerp(new Color(1f, 0.8f, 0.25f), new Color(0.95f, 0.25f, 0.2f), (n - 0.5f) * 2f);
            _temperatureValue.text = $"{_temperature.Current:0.00}°C  /  limit {_temperature.Threshold:0.0}°C";

            _oxygenValue.text = $"{_oxygen.Current} / {_oxygen.Allowance}";
            _waveValue.text = $"WAVE {_waves.WaveNumber} / {_waves.TotalWaves}   ·   TURN {_waves.TurnInWave}";

            bool canAct = GameManager.Instance != null && GameManager.Instance.CanPlayerAct;
            _endTurnButton.interactable = canAct;
            foreach (DeployCard card in _cards)
            {
                if (card.Data == null) continue;
                card.Refresh(card.Data == _deploy.Selected, _oxygen.CanAfford(card.Data.opCost), canAct);
            }
        }

        public void Announce(string message, Color color)
        {
            if (_announceRoutine != null) StopCoroutine(_announceRoutine);
            _announceRoutine = StartCoroutine(AnnounceRoutine(message, color));
        }

        private IEnumerator AnnounceRoutine(string message, Color color)
        {
            _announcement.text = message;
            _announcement.gameObject.SetActive(true);
            for (float t = 0f; t < 3.2f; t += Time.deltaTime)
            {
                Color c = color;
                c.a = t < 2.6f ? 1f : 1f - (t - 2.6f) / 0.6f;
                _announcement.color = c;
                yield return null;
            }
            _announcement.gameObject.SetActive(false);
        }

        /// <summary>Damage numbers and °C changes that float up from a unit.</summary>
        public void ShowFloatingText(Vector3 worldPos, string text, Color color)
        {
            if (Camera.main == null) return;
            StartCoroutine(FloatRoutine(worldPos, text, color));
        }

        private IEnumerator FloatRoutine(Vector3 worldPos, string value, Color color)
        {
            var go = new GameObject("FloatingText", typeof(RectTransform));
            go.transform.SetParent(_floatingLayer, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(220f, 50f);

            Vector3 start = Camera.main.WorldToScreenPoint(worldPos);
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                rt.position = start + Vector3.up * (70f * t);
                Color c = color; c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                text.color = c;
                yield return null;
            }
            Destroy(go);
        }

        public void FlashTemperature()
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            Color normal = new Color(0.1f, 0.12f, 0.16f, 0.95f);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                _temperatureBack.color = Color.Lerp(new Color(0.8f, 0.15f, 0.1f, 1f), normal, t / 0.6f);
                yield return null;
            }
            _temperatureBack.color = normal;
        }
    }
}
