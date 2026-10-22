using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>Settings dialog: audio, display, gameplay and data. Every change is saved immediately (PlayerPrefs).</summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private MainMenuController _menu;
        [SerializeField] private GameObject _root;
        [SerializeField] private Slider _music;
        [SerializeField] private Text _musicValue;
        [SerializeField] private Slider _sfx;
        [SerializeField] private Text _sfxValue;
        [SerializeField] private Toggle _largeText;
        [SerializeField] private Toggle _highContrast;
        [SerializeField] private Button[] _speedButtons = new Button[3];
        [SerializeField] private Toggle _popups;
        [SerializeField] private Toggle _numbers;
        [SerializeField] private Toggle _bossWarning;
        [SerializeField] private Toggle _laneCamera;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _creditsButton;
        [SerializeField] private Button _resultsButton;
        [SerializeField] private Button _exportButton;
        [SerializeField] private Text _dataNote;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _doneButton;

        private static readonly Color Gold = new Color(0.886f, 0.682f, 0.173f);
        private static readonly Color Cream = new Color(0.953f, 0.918f, 0.843f);
        private static readonly Color Ink = new Color(0.102f, 0.078f, 0.039f);

        private void Awake()
        {
            _music.onValueChanged.AddListener(v => { SettingsStore.MusicVolume = v; Labels(); });
            _sfx.onValueChanged.AddListener(v => { SettingsStore.SfxVolume = v; Labels(); });
            _largeText.onValueChanged.AddListener(v => SettingsStore.LargeText = v);
            _highContrast.onValueChanged.AddListener(v => SettingsStore.HighContrast = v);
            _popups.onValueChanged.AddListener(v => SettingsStore.PopupsEnabled = v);
            _numbers.onValueChanged.AddListener(v => SettingsStore.ShowNumbers = v);
            _bossWarning.onValueChanged.AddListener(v => SettingsStore.BossWarning = v);
            if (_laneCamera != null) _laneCamera.onValueChanged.AddListener(v => SettingsStore.LaneCamera = v);
            for (int i = 0; i < _speedButtons.Length; i++)
            {
                int index = i;
                _speedButtons[i].onClick.AddListener(() => { SettingsStore.BattleSpeedIndex = index; Speeds(); });
            }
            _resetButton.onClick.AddListener(OnReset);
            _creditsButton.onClick.AddListener(() => _menu.OpenCredits());
            _resultsButton.onClick.AddListener(() => _menu.OpenResults());
            if (_exportButton != null) _exportButton.onClick.AddListener(OnExport);
            _closeButton.onClick.AddListener(Close);
            _doneButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            _music.SetValueWithoutNotify(SettingsStore.MusicVolume);
            _sfx.SetValueWithoutNotify(SettingsStore.SfxVolume);
            _largeText.SetIsOnWithoutNotify(SettingsStore.LargeText);
            _highContrast.SetIsOnWithoutNotify(SettingsStore.HighContrast);
            _popups.SetIsOnWithoutNotify(SettingsStore.PopupsEnabled);
            _numbers.SetIsOnWithoutNotify(SettingsStore.ShowNumbers);
            _bossWarning.SetIsOnWithoutNotify(SettingsStore.BossWarning);
            if (_laneCamera != null) _laneCamera.SetIsOnWithoutNotify(SettingsStore.LaneCamera);
            Labels();
            Speeds();
            bool named = _menu.NameIsValid;
            _resetButton.interactable = named;
            _dataNote.text = named
                ? $"Reset clears {_menu.PlayerName}'s saved game and unlocked levels. Results, quiz answers and found facts stay."
                : "Type your player name on the main menu to reset that player's progress.";
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Close() => _root.SetActive(false);

        private void Labels()
        {
            _musicValue.text = $"{Mathf.RoundToInt(_music.value * 100)}%";
            _sfxValue.text = $"{Mathf.RoundToInt(_sfx.value * 100)}%";
        }

        private void Speeds()
        {
            for (int i = 0; i < _speedButtons.Length; i++)
            {
                bool on = i == SettingsStore.BattleSpeedIndex;
                _speedButtons[i].GetComponent<Image>().color = on ? Gold : new Color(0, 0, 0, 0);
                var label = _speedButtons[i].GetComponentInChildren<Text>();
                if (label != null) label.color = on ? Ink : Cream;
            }
        }

        /// <summary>Writes the quiz-results CSV for the study (Documents/ThermoTactics) and shows where it went.</summary>
        private void OnExport()
        {
            QuizResultExporter.ExportResult r = QuizResultExporter.Export(_menu.Database, _menu.Stage, SettingsStore.TesterCode);
            _dataNote.text = r.Success ? $"<color=#9BD3B0>{r.Message}</color>" : $"<color=#FF9A86>{r.Message}</color>";
        }

        private void OnReset()
        {
            string name = _menu.PlayerName;
            _menu.Confirm("RESET PROGRESS?",
                $"This deletes {name}'s saved game and locks Baguio Levels 2–10 again. The Level 1 tutorial shows again. Results and quiz answers stay.",
                "RESET",
                () =>
                {
                    _menu.Database.ResetProgress(name);
                    SettingsStore.SetTutorialDone(name, false);
                    _menu.RefreshProfile();
                    _dataNote.text = $"{name}'s progress was reset.";
                });
        }
    }
}
