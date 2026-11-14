using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// The Main Menu scene: player profile form, Continue / Start / Settings / Quit, and the shared dialogs
    /// (quit, credits, "replace saved game?"). Continue is enabled only when the typed player has a saved game
    /// in the saved_session table; it then reopens the battle at the same stage, level, wave and turn.
    /// Stages: Baguio Forest is open from the start; each later stage opens once every level of the stage before it
    /// is cleared. The "current" stage is the one the player picked, else the stage of the saved game, else the
    /// newest stage they have opened.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("Every built stage, in order (Stage 1 first).")]
        [SerializeField] private StageData[] _stages = new StageData[0];
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
        [SerializeField] private Text _progressTitle;
        [SerializeField] private Text _progressStars;
        [SerializeField] private Text _progressInfo;
        [SerializeField] private Image[] _progressSegments = new Image[10];

        [Header("Carbon Credits, store and squad")]
        [SerializeField] private GameObject _walletRoot;
        [SerializeField] private Text _walletText;
        [SerializeField] private Button _storeButton;
        [SerializeField] private GameObject _storeOn;
        [SerializeField] private GameObject _storeOff;
        [SerializeField] private StorePanel _store;
        [SerializeField] private SquadPanel _squad;

        [Header("Panels")]
        [SerializeField] private SettingsPanel _settings;
        [SerializeField] private StageSelectPanel _stageSelect;
        [SerializeField] private LevelSelectPanel _levelSelect;
        [SerializeField] private ResultsPanel _results;
        [SerializeField] private QuizPanel _quiz;
        [SerializeField] private FactsPanel _facts;

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
        private StageData _stage;
        private bool _stagePicked;   // true once the player chose a stage (or came back from one); cleared on the title

        /// <summary>The stage the menus show (level path, squad, facts).</summary>
        public StageData Stage => _stage;
        public StageData[] Stages => _stages;
        public StageData FirstStage => _stages != null && _stages.Length > 0 ? _stages[0] : null;
        public ResultsDatabase Database => _database;
        public string PlayerName => ResultsDatabase.Sanitize(_nameInput.text);
        public bool NameIsValid => PlayerName.Length >= 2;
        /// <summary>The player's saved game, in any stage.</summary>
        public SessionSnapshot SavedGame => _save;
        /// <summary>The saved game if it belongs to the current stage, else null.</summary>
        public SessionSnapshot SavedGameHere => _save != null && _stage != null && _save.stageId == _stage.stageId ? _save : null;

        public StageData FindStage(int stageId)
        {
            if (_stages == null) return null;
            foreach (StageData s in _stages) if (s != null && s.stageId == stageId) return s;
            return null;
        }

        /// <summary>Stage 1 is always open; a later stage opens when every level of the stage before it is cleared.</summary>
        public bool IsStageOpen(int stageId)
        {
            if (stageId <= 1) return FindStage(stageId) != null;
            StageData prev = FindStage(stageId - 1);
            return FindStage(stageId) != null && prev != null && NameIsValid
                   && _database.CountCleared(PlayerName, prev.stageId) >= prev.LevelCount;
        }

        /// <summary>True when every level of the stage is cleared.</summary>
        public bool IsStageDone(StageData stage) => stage != null && NameIsValid && _database.CountCleared(PlayerName, stage.stageId) >= stage.LevelCount;

        /// <summary>The newest open stage (Stage 1 for a new player).</summary>
        public StageData NewestOpenStage
        {
            get
            {
                StageData best = FirstStage;
                if (_stages != null) foreach (StageData s in _stages) if (s != null && IsStageOpen(s.stageId)) best = s;
                return best;
            }
        }

        /// <summary>The highest stage whose allies are sold: one past the last fully cleared stage (at least 1).</summary>
        public int StoreStage
        {
            get
            {
                int stage = 1;
                if (_stages != null) foreach (StageData s in _stages) if (s != null && IsStageDone(s)) stage = Mathf.Max(stage, s.stageId + 1);
                return stage;
            }
        }

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
            if (_storeButton != null) _storeButton.onClick.AddListener(OpenStore);
            _quitButton.onClick.AddListener(OpenQuit);
            _quitStay.onClick.AddListener(() => _quitDialog.SetActive(false));
            _quitConfirm.onClick.AddListener(QuitGame);
            _confirmCancel.onClick.AddListener(() => _confirmDialog.SetActive(false));
            _confirmOk.onClick.AddListener(() => { _confirmDialog.SetActive(false); _onConfirm?.Invoke(); });
            _creditsClose.onClick.AddListener(() => _creditsDialog.SetActive(false));
            int built = _stages != null ? _stages.Length : 0;
            _versionText.text = built > 1 ? $"v{Application.version} · Stages 1–{built}" : $"v{Application.version} · Stage 1 prototype";
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
            if (_facts != null) _facts.Hide();
            // Back from a battle: stay in that stage.
            if (GameSession.Stage != null && FindStage(GameSession.Stage.stageId) != null && (GameSession.OpenLevelSelect || GameSession.OpenStageSelect))
            {
                _stage = FindStage(GameSession.Stage.stageId);
                _stagePicked = true;
            }
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
            _stagePicked = false;
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

        /// <summary>A stage card: makes it the current stage, then enters it.</summary>
        public void EnterStage(StageData stage)
        {
            if (stage == null || !IsStageOpen(stage.stageId)) return;
            _stage = stage;
            _stagePicked = true;
            EnterStage();
        }

        /// <summary>
        /// Enters the current stage: the pre-quiz first if this player has not taken it, then the level path. If the stage
        /// was finished but the post-quiz was never submitted (the app closed during it), the post-quiz comes first instead.
        /// A stage without a quiz goes straight to the level path.
        /// </summary>
        public void EnterStage()
        {
            string name = PlayerName;
            if (_quiz != null && QuizManager.NeedsPre(_database, name, _stage))
            {
                _quiz.Show(_stage, QuizManager.Pre, name, _database, _ => ShowLevelSelect());
                return;
            }
            if (_quiz != null && _database.CountCleared(name, _stage.stageId) >= _stage.LevelCount && QuizManager.NeedsPost(_database, name, _stage))
            {
                _quiz.Show(_stage, QuizManager.Post, name, _database, _ => ShowLevelSelect());
                return;
            }
            ShowLevelSelect();
        }

        public void OpenFacts()
        {
            if (_facts != null) _facts.Show(_stage, PlayerName, _database);
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
            StageData saveStage = _save != null ? FindStage(_save.stageId) : null;
            if (_save != null && (saveStage == null || saveStage.GetLevel(_save.levelNumber) == null)) { _save = null; saveStage = null; }
            if (!_stagePicked || _stage == null) _stage = saveStage != null ? saveStage : NewestOpenStage;
            bool canContinue = _save != null;

            _continueOn.SetActive(canContinue);
            _continueOff.SetActive(!canContinue);
            _continueButton.interactable = canContinue;
            var shadow = _continueButton.GetComponent<Shadow>();
            if (shadow != null) shadow.enabled = canContinue;   // the disabled (dashed) state has no drop shadow
            int cleared = NameIsValid ? _database.CountCleared(name, _stage.stageId) : 0;

            if (canContinue)
            {
                LevelData lv = saveStage.GetLevel(_save.levelNumber);
                _continueDetail.text = $"{saveStage.ShortName} Level {_save.levelNumber} · Wave {_save.waveNumber} of {lv.waves.Length} · Turn {_save.turnNumber} · {_save.temperature:0.00} °C";
                _continueSaved.text = $"Saved {_save.FriendlySavedAt()} — resumes at Wave {_save.waveNumber}, Turn {_save.turnNumber}";
            }

            _infoSaved.SetActive(canContinue);
            _infoNew.SetActive(!canContinue);
            string where = _stage.ShortName;
            string clearedText = cleared == 0 ? $"No {where} levels cleared yet." : cleared == 1 ? $"{where} Level 1 cleared." : $"{where} Levels 1–{cleared} cleared.";
            if (canContinue)
                _infoSavedText.text = $"Saved session found for <b>{Escape(name)}</b>. {clearedText} Press <b>Continue</b> to resume.";
            else if (!NameIsValid)
                _infoNewText.text = "Type your name (2–20 characters), then press <b>Start</b>.";
            else if (cleared > 0)
                _infoNewText.text = $"Welcome back, <b>{Escape(name)}</b>. {clearedText} Press <b>Start</b> to pick a level.";
            else
                _infoNewText.text = $"New player. Press <b>Start</b> to begin in {FirstStage.stageName}. Your progress saves automatically on this phone.";
            _nameMessage.text = "";
            RefreshProgress(name);
            RefreshWallet(name);
        }

        /// <summary>
        /// The Carbon Credits count (top right, hidden until a valid name is typed) and the STORE button under Start,
        /// which stays grey and locked until the player has cleared Baguio Level 1.
        /// </summary>
        public void RefreshWallet(string name)
        {
            if (_walletRoot != null)
            {
                _walletRoot.SetActive(NameIsValid);
                if (NameIsValid && _walletText != null) _walletText.text = CreditRules.Format(_database.GetCredits(name));
            }
            if (_levelSelect != null) _levelSelect.RefreshStore();
            bool open = StoreUnlocked;
            if (_storeOn != null) _storeOn.SetActive(open);
            if (_storeOff != null) _storeOff.SetActive(!open);
            if (_storeButton != null)
            {
                _storeButton.interactable = open;
                var shadow = _storeButton.GetComponent<Shadow>();
                if (shadow != null) shadow.enabled = open;
            }
        }

        /// <summary>The Store opens once the player has cleared Level 1 of the first stage (Baguio).</summary>
        public bool StoreUnlocked => NameIsValid && _database != null && FirstStage != null && _database.CountCleared(PlayerName, FirstStage.stageId) >= 1;

        /// <summary>The current stage's strip on the title: one block per level (gold = cleared, red = next) and the star total.</summary>
        private void RefreshProgress(string name)
        {
            if (_progressRoot == null) return;
            _progressRoot.SetActive(NameIsValid);
            if (!NameIsValid) return;
            if (_progressTitle != null) _progressTitle.text = $"STAGE {_stage.stageId} · {_stage.stageName.ToUpperInvariant()}";
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
            StageData saveStage = FindStage(_save.stageId);
            if (saveStage == null) return;
            _stage = saveStage;
            PlayLevel(_save.levelNumber, _save);
        }

        /// <summary>Loads the battle scene. With a snapshot, the board is rebuilt exactly as saved (with its squad).</summary>
        public void PlayLevel(int levelNumber, SessionSnapshot resume, UnitData[] squad = null)
        {
            GameSession.Play(PlayerName, _stage, levelNumber, resume, squad);
            SceneManager.LoadScene(GameSession.BattleScene);
        }

        /// <summary>The Store (Carbon Credits → new allies). Needs a valid player name.</summary>
        public void OpenStore()
        {
            if (!NameIsValid)
            {
                _nameMessage.text = "Type your name first: Carbon Credits and bought allies are saved per player.";
                return;
            }
            if (!StoreUnlocked) return;
            if (_store != null) _store.Show(this);
        }

        /// <summary>
        /// Squad pick, then the level. The first time a player starts Level 1 the tutorial uses the starters,
        /// so the squad screen is skipped.
        /// </summary>
        private void ChooseSquadThenPlay(int levelNumber)
        {
            if (_squad == null || (levelNumber == 1 && _stage.stageId == 1 && TutorialController.IsDue(PlayerName)))
            {
                PlayLevel(levelNumber, null);
                return;
            }
            string player = PlayerName;
            _squad.Show(this, levelNumber, squad =>
            {
                PlayerSquad.Save(player, squad);
                PlayLevel(levelNumber, null, squad);
            });
        }

        /// <summary>Starts a level fresh (after the squad pick). If another saved game exists, asks before replacing it.</summary>
        public void StartFresh(int levelNumber)
        {
            if (_save == null)
            {
                ChooseSquadThenPlay(levelNumber);
                return;
            }
            Confirm("REPLACE SAVED GAME?",
                $"You have a saved game at {SavedStageName} Level {_save.levelNumber}, Wave {_save.waveNumber}, Turn {_save.turnNumber}. Starting {_stage.ShortName} Level {levelNumber} deletes it.",
                "REPLACE",
                () =>
                {
                    _database.DeleteSession(PlayerName);
                    _save = null;
                    ChooseSquadThenPlay(levelNumber);
                });
        }

        private string SavedStageName => _save != null && FindStage(_save.stageId) is StageData s ? s.ShortName : "";

        // ---------------------------------------------------------------- quit

        private void OpenQuit()
        {
            RefreshProfile();
            _quitInfo.text = _save != null
                ? $"Your progress is saved. Next time, press <b>Continue</b> to return to {SavedStageName} Level {_save.levelNumber}, Wave {_save.waveNumber}, Turn {_save.turnNumber}."
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
