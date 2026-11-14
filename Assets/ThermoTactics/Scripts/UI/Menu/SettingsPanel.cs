using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// Settings dialog: audio, display, gameplay, data and admin. Every change is saved immediately (PlayerPrefs).
    /// Players can open the leaderboard, see the credits and reset their own progress. The full Results table, the quiz
    /// CSV export and the phone letter (the first letter of new player codes) stay greyed out until the admin code is
    /// entered (see <see cref="AdminSession"/>).
    /// </summary>
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
        [SerializeField] private Button _leaderboardButton;
        [Header("Admin only")]
        [SerializeField] private InputField _adminInput;
        [SerializeField] private Button _adminButton;
        [SerializeField] private Text _adminButtonLabel;
        [SerializeField] private Text _adminStatus;
        [SerializeField] private Button _resultsButton;
        [SerializeField] private Button _exportButton;
        [SerializeField] private Button _phoneLetterButton;
        [SerializeField] private Text _phoneLetterLabel;
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
            _leaderboardButton.onClick.AddListener(() => _menu.OpenLeaderboard());
            _resultsButton.onClick.AddListener(() => { if (AdminSession.Unlocked) _menu.OpenAdminResults(); });
            _exportButton.onClick.AddListener(OnExport);
            _phoneLetterButton.onClick.AddListener(OnPhoneLetter);
            _adminButton.onClick.AddListener(OnAdmin);
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
                ? $"Reset clears {_menu.PlayerName}'s saved game, unlocked levels, Carbon Credits and bought allies. Results, quiz answers and found facts stay."
                : "Type your player name on the main menu to reset that player's progress.";
            _adminInput.SetTextWithoutNotify("");
            _adminStatus.text = "";
            RefreshAdmin();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }

        public void Close() => _root.SetActive(false);

        private int _shownWait = -1;

        private void Update()
        {
            // Counts down the wait after 3 wrong codes.
            if (AdminSession.Unlocked) return;
            int wait = AdminSession.WaitLeft;
            if (wait == _shownWait) return;
            _shownWait = wait;
            if (wait > 0) Status($"Too many wrong codes. Try again in {wait} s.", "#FF9A86");
            else if (_adminStatus.text.Contains("Too many")) Status("", null);
            _adminButton.interactable = wait == 0;
        }

        // ---------------------------------------------------------------- admin

        private void OnAdmin()
        {
            if (AdminSession.Unlocked)
            {
                AdminSession.Lock();
                Status("Admin tools locked.", null);
                RefreshAdmin();
                return;
            }
            if (AdminSession.WaitLeft > 0) return;
            string code = _adminInput.text;
            _adminInput.SetTextWithoutNotify("");
            if (string.IsNullOrWhiteSpace(code)) { Status("Type the admin code first.", "#FF9A86"); return; }
            if (AdminSession.TryUnlock(code)) Status("Admin unlocked until the app is closed.", "#9BD3B0");
            else if (AdminSession.WaitLeft == 0) Status("Wrong admin code.", "#FF9A86");
            _shownWait = -1;
            RefreshAdmin();
        }

        /// <summary>Greys out the admin-only buttons while locked and flips UNLOCK / LOCK.</summary>
        private void RefreshAdmin()
        {
            bool on = AdminSession.Unlocked;
            foreach (Button b in new[] { _resultsButton, _exportButton, _phoneLetterButton })
            {
                b.interactable = on;
                var group = b.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = on ? 1f : 0.4f;
            }
            _adminInput.interactable = !on;
            if (_adminInput.placeholder is Text ph) ph.text = on ? "Unlocked" : "Enter code";
            _adminButtonLabel.text = on ? "LOCK" : "UNLOCK";
            _adminButton.interactable = on || AdminSession.WaitLeft == 0;
            _phoneLetterLabel.text = $"Phone: {SettingsStore.PhoneLetter}";
        }

        private void Status(string text, string hex)
            => _adminStatus.text = string.IsNullOrEmpty(hex) || text.Length == 0 ? text : $"<color={hex}>{text}</color>";

        /// <summary>Next letter (A to Z, then back to A). Only players registered from now on get the new letter.</summary>
        private void OnPhoneLetter()
        {
            if (!AdminSession.Unlocked) return;
            char c = SettingsStore.PhoneLetter[0];
            SettingsStore.PhoneLetter = c == 'Z' ? "A" : ((char)(c + 1)).ToString();
            RefreshAdmin();
            _dataNote.text = $"New players on this phone now get codes like <b>{SettingsStore.PhoneLetter}-001</b>. Use a different letter on each phone. Players already registered keep their codes.";
            _menu.RefreshProfile();
        }

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
            if (!AdminSession.Unlocked) return;
            QuizResultExporter.ExportResult r = QuizResultExporter.Export(_menu.Database, _menu.FirstStage);
            _dataNote.text = r.Success ? $"<color=#9BD3B0>{r.Message}</color>" : $"<color=#FF9A86>{r.Message}</color>";
        }

        private void OnReset()
        {
            string name = _menu.PlayerName;
            _menu.Confirm("RESET PROGRESS?",
                $"This deletes {name}'s saved game, Carbon Credits and bought allies, and locks every level after Baguio Level 1 again. The Level 1 tutorial shows again. Results and quiz answers stay.",
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
