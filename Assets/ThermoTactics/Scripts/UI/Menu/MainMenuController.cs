using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// The Main Menu scene: player profile form, Continue / Start / Settings / Quit, and the shared dialogs
    /// (quit, credits, "replace saved game?"). Continue is enabled only when the typed player has a saved game
    /// in the saved_session table; it then reopens the battle at the same level, wave and turn.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private StageData _stage;
        [SerializeField] private ResultsDatabase _database;

        [Header("Title screen")]
        [SerializeField] private GameObject _titleScreen;
        [SerializeField] private InputField _nameInput;
        [SerializeField] private InputField _testerInput;
        [SerializeField] private Text _nameMessage;
        [SerializeField] private Button _continueButton;
        [SerializeField] private GameObject _continueOn;
        [SerializeField] private GameObject _continueOff;
        [SerializeField] private Text _continueDetail;
        [SerializeField] private Text _continueSaved;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private GameObject _infoSaved;
        [SerializeField] private Text _infoSavedText;
        [SerializeField] private GameObject _infoNew;
        [SerializeField] private Text _infoNewText;
        [SerializeField] private Text _versionText;

        [Header("Stage progress strip (title screen)")]
        [SerializeField] private GameObject _progressRoot;
        [SerializeField] private Text _progressStars;
        [SerializeField] private Text _progressInfo;
        [SerializeField] private Image[] _progressSegments = new Image[10];

        [Header("Panels")]
        [SerializeField] private SettingsPanel _settings;
        [SerializeField] private StageSelectPanel _stageSelect;
        [SerializeField] private LevelSelectPanel _levelSelect;
        [SerializeField] private ResultsPanel _results;

        [Header("Quit dialog")]
        [SerializeField] private GameObject _quitDialog;
        [SerializeField] private Text _quitInfo;
        [SerializeField] private Button _quitStay;
        [SerializeField] private Button _quitConfirm;

        [Header("Confirm dialog")]
        [SerializeField] private GameObject _confirmDialog;
        [SerializeField] private Text _confirmTitle;
        [SerializeField] private Text _confirmBody;
        [SerializeField] private Button _confirmCancel;
        [SerializeField] private Button _confirmOk;
        [SerializeField] private Text _confirmOkLabel;

        [Header("Credits")]
        [SerializeField] private GameObject _creditsDialog;
        [SerializeField] private Button _creditsClose;

        private Action _onConfirm;
        private SessionSnapshot _save;

        public StageData Stage => _stage;
        public ResultsDatabase Database => _database;
        public string PlayerName => ResultsDatabase.Sanitize(_nameInput.text);
        public bool NameIsValid => PlayerName.Length >= 2;
        public SessionSnapshot SavedGame => _save;

        private void Awake()
        {
            Time.timeScale = 1f;
            SettingsStore.Apply();
            _nameInput.characterLimit = ResultsDatabase.MaxNameLength;
            _testerInput.characterLimit = 8;
            _nameInput.onValueChanged.AddListener(_ => RefreshProfile());
            _testerInput.onEndEdit.AddListener(v => SettingsStore.TesterCode = v.Trim());
            _continueButton.onClick.AddListener(OnContinue);
            _startButton.onClick.AddListener(OnStart);
            _settingsButton.onClick.AddListener(() => _settings.Open());
            _quitButton.onClick.AddListener(OpenQuit);
            _quitStay.onClick.AddListener(() => _quitDialog.SetActive(false));
            _quitConfirm.onClick.AddListener(QuitGame);
            _confirmCancel.onClick.AddListener(() => _confirmDialog.SetActive(false));
            _confirmOk.onClick.AddListener(() => { _confirmDialog.SetActive(false); _onConfirm?.Invoke(); });
            _creditsClose.onClick.AddListener(() => _creditsDialog.SetActive(false));
            _versionText.text = $"v{Application.version} · Stage 1 prototype";
        }

        private void Start()
        {
            string name = !string.IsNullOrEmpty(GameSession.PlayerName) ? GameSession.PlayerName : SettingsStore.LastPlayer;
            _nameInput.SetTextWithoutNotify(name);
            _testerInput.SetTextWithoutNotify(SettingsStore.TesterCode);
            _quitDialog.SetActive(false);
            _confirmDialog.SetActive(false);
            _creditsDialog.SetActive(false);
            _settings.Close();
            _results.Hide();
            RefreshProfile();

            if (GameSession.OpenLevelSelect && NameIsValid)
            {
                GameSession.OpenLevelSelect = false;
                ShowLevelSelect(GameSession.LevelNumber);
            }
            else if (GameSession.OpenStageSelect && NameIsValid)
            {
                GameSession.OpenStageSelect = false;
                ShowStageSelect();
            }
            else ShowTitle();
            GameSession.OpenLevelSelect = GameSession.OpenStageSelect = false;
        }

        // ---------------------------------------------------------------- navigation

        public void ShowTitle()
        {
            _stageSelect.Hide();
            _levelSelect.Hide();
            _titleScreen.SetActive(true);
            RefreshProfile();
        }

        public void ShowStageSelect()
        {
            _titleScreen.SetActive(false);
            _levelSelect.Hide();
            _stageSelect.Show(this);
        }

        public void ShowLevelSelect(int selectLevel = 0)
        {
            _titleScreen.SetActive(false);
            _stageSelect.Hide();
            _levelSelect.Show(this, selectLevel);
        }

        public void OpenResults() => _results.Show(_database.GetAll(), _database.FilePath);

        public void OpenCredits()
        {
            _creditsDialog.SetActive(true);
            _creditsDialog.transform.SetAsLastSibling();
        }

        /// <summary>Two-button confirmation, e.g. "Replace saved game?".</summary>
        public void Confirm(string title, string body, string okLabel, Action onOk)
        {
            _confirmTitle.text = title;
            _confirmBody.text = body;
            _confirmOkLabel.text = okLabel;
            _onConfirm = onOk;
            _confirmDialog.SetActive(true);
            _confirmDialog.transform.SetAsLastSibling();
        }

        // ---------------------------------------------------------------- profile

        public void RefreshProfile()
        {
            string name = PlayerName;
            _save = NameIsValid ? _database.LoadSession(name) : null;
            if (_save != null && (_stage == null || _save.stageId != _stage.stageId || _stage.GetLevel(_save.levelNumber) == null)) _save = null;
            bool canContinue = _save != null;

            _continueOn.SetActive(canContinue);
            _continueOff.SetActive(!canContinue);
            _continueButton.interactable = canContinue;
            var shadow = _continueButton.GetComponent<Shadow>();
            if (shadow != null) shadow.enabled = canContinue;   // the disabled (dashed) state has no drop shadow
            int cleared = NameIsValid ? _database.CountCleared(name, _stage.stageId) : 0;

            if (canContinue)
            {
                LevelData lv = _stage.GetLevel(_save.levelNumber);
                _continueDetail.text = $"Baguio Level {_save.levelNumber} · Wave {_save.waveNumber} of {lv.waves.Length} · Turn {_save.turnNumber} · {_save.temperature:0.00} °C";
                _continueSaved.text = $"Saved {_save.FriendlySavedAt()} — resumes at Wave {_save.waveNumber}, Turn {_save.turnNumber}";
            }

            _infoSaved.SetActive(canContinue);
            _infoNew.SetActive(!canContinue);
            string clearedText = cleared == 0 ? "No Baguio levels cleared yet." : cleared == 1 ? "Baguio Level 1 cleared." : $"Baguio Levels 1–{cleared} cleared.";
            if (canContinue)
                _infoSavedText.text = $"Saved session found for <b>{Escape(name)}</b>. {clearedText} Press <b>Continue</b> to resume.";
            else if (!NameIsValid)
                _infoNewText.text = "Type your name (2–20 characters), then press <b>Start</b>.";
            else if (cleared > 0)
                _infoNewText.text = $"Welcome back, <b>{Escape(name)}</b>. {clearedText} Press <b>Start</b> to pick a level.";
            else
                _infoNewText.text = "New player. Press <b>Start</b> to begin in Baguio Forest. Your progress saves automatically on this phone.";
            _nameMessage.text = "";
            RefreshProgress(name);
        }

        /// <summary>The Baguio strip on the title: one block per level (gold = cleared, red = next) and the star total.</summary>
        private void RefreshProgress(string name)
        {
            if (_progressRoot == null) return;
            _progressRoot.SetActive(NameIsValid);
            if (!NameIsValid) return;
            var progress = new System.Collections.Generic.Dictionary<int, LevelProgress>();
            int stars = 0;
            foreach (LevelProgress p in _database.GetProgress(name, _stage.stageId))
            {
                progress[p.LevelNumber] = p;
                stars += StarRating.For(p);
            }
            int levels = _stage.LevelCount, next = 0, cleared = 0;
            for (int i = 0; i < _progressSegments.Length; i++)
            {
                int level = i + 1;
                bool done = progress.TryGetValue(level, out LevelProgress p) && p.IsCleared == 1;
                if (done) cleared++;
                else if (next == 0 && level <= levels) next = level;
                _progressSegments[i].gameObject.SetActive(level <= levels);
                _progressSegments[i].color = done ? new Color(0.886f, 0.682f, 0.173f)
                    : level == next ? new Color(0.64f, 0.14f, 0.11f)
                    : new Color(0.133f, 0.2f, 0.165f);
            }
            _progressStars.text = $"{stars} / {levels * StarRating.MaxStars}";
            LevelData nl = next > 0 ? _stage.GetLevel(next) : null;
            _progressInfo.text = cleared >= levels ? $"All {levels} levels cleared! Replay levels for more stars."
                : nl != null ? $"{cleared} of {levels} cleared · Next: Level {next}, {nl.locationName}"
                : $"{cleared} of {levels} cleared";
        }

        private static string Escape(string s) => s.Replace("<", "‹").Replace(">", "›");

        private bool CheckName()
        {
            string name = PlayerName;
            if (name.Length == 0) { _nameMessage.text = "Please enter your player name."; return false; }
            if (name.Length < 2) { _nameMessage.text = "Use at least 2 characters."; return false; }
            SettingsStore.LastPlayer = name;
            SettingsStore.TesterCode = _testerInput.text.Trim();
            GameSession.PlayerName = name;
            return true;
        }

        private void OnStart()
        {
            if (!CheckName()) return;
            ShowStageSelect();
        }

        private void OnContinue()
        {
            if (!CheckName() || _save == null) return;
            PlayLevel(_save.levelNumber, _save);
        }

        /// <summary>Loads the battle scene. With a snapshot, the board is rebuilt exactly as saved.</summary>
        public void PlayLevel(int levelNumber, SessionSnapshot resume)
        {
            GameSession.Play(PlayerName, _stage, levelNumber, resume);
            SceneManager.LoadScene(GameSession.BattleScene);
        }

        /// <summary>Starts a level fresh. If another saved game exists, asks before replacing it.</summary>
        public void StartFresh(int levelNumber)
        {
            if (_save == null)
            {
                PlayLevel(levelNumber, null);
                return;
            }
            Confirm("REPLACE SAVED GAME?",
                $"You have a saved game at Baguio Level {_save.levelNumber}, Wave {_save.waveNumber}, Turn {_save.turnNumber}. Starting Level {levelNumber} deletes it.",
                "REPLACE",
                () =>
                {
                    _database.DeleteSession(PlayerName);
                    _save = null;
                    PlayLevel(levelNumber, null);
                });
        }

        // ---------------------------------------------------------------- quit

        private void OpenQuit()
        {
            RefreshProfile();
            _quitInfo.text = _save != null
                ? $"Your progress is saved. Next time, press <b>Continue</b> to return to Baguio Level {_save.levelNumber}, Wave {_save.waveNumber}, Turn {_save.turnNumber}."
                : "Your cleared levels are saved on this phone.";
            _quitDialog.SetActive(true);
            _quitDialog.transform.SetAsLastSibling();
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
