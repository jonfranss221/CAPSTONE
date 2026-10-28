using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThermoTactics
{
    /// <summary>
    /// Orchestrates one level in the battle scene: start or resume → play → win/loss → results.
    /// The level comes from GameSession (chosen in the Main Menu). Saves the game at the start of
    /// every player turn, after every deploy, and when the app is paused or closed, so Continue
    /// returns to the same wave and turn.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum State { Loading, Playing, Paused, Ended }

        public static GameManager Instance { get; private set; }

        [Tooltip("Used when the battle scene is played directly in the editor (no menu choice).")]
        [SerializeField] private StageData _defaultStage;
        [SerializeField] private TurnManager _turns;
        [SerializeField] private WaveManager _waves;
        [SerializeField] private OxygenPointSystem _oxygen;
        [SerializeField] private TemperatureSystem _temperature;
        [SerializeField] private GridManager _grid;
        [SerializeField] private LaneController _lanes;
        [SerializeField] private DeploySystem _deploy;
        [SerializeField] private UnitFactory _factory;
        [SerializeField] private ResultsDatabase _database;
        [SerializeField] private UIManager _ui;
        [SerializeField] private FogScroller _fog;
        [SerializeField] private BattleCamera _camera;
        [SerializeField] private PopupManager _popups;
        [SerializeField] private LevelIntro _intro;
        [SerializeField] private TutorialController _tutorial;

        public State Current { get; private set; } = State.Loading;
        public StageData Stage { get; private set; }
        public LevelData Level { get; private set; }
        public int LevelNumber { get; private set; } = 1;
        public string PlayerName { get; private set; } = "";
        public bool CanPlayerAct => Current == State.Playing && !_turns.IsResolving && !PopupManager.IsOpen && !LevelIntro.IsPlaying;
        public PopupManager Popups => _popups;
        public bool HasNextLevel => Stage != null && LevelNumber < Stage.LevelCount;

        private Coroutine _opening;

        /// <summary>True when the last autosave reached the database.</summary>
        public bool LastSaveOk { get; private set; } = true;

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            SettingsStore.Apply();
        }

        private void Start()
        {
            _temperature.Changed += OnTemperatureChanged;
            _deploy.Deployed += _ => Autosave();

            Stage = GameSession.Stage != null ? GameSession.Stage : _defaultStage;
            PlayerName = ResultsDatabase.Sanitize(string.IsNullOrEmpty(GameSession.PlayerName) ? "Tester" : GameSession.PlayerName);
            LevelNumber = Mathf.Clamp(GameSession.LevelNumber, 1, Mathf.Max(1, Stage.LevelCount));

            SessionSnapshot resume = GameSession.Resume;
            GameSession.Resume = null;
            if (resume != null && ResumeLevel(resume)) return;
            StartLevel(LevelNumber);
            if (resume != null)
                _ui.Announce("The saved game could not be restored, so the level starts from Wave 1.", UIManager.Warning);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- start / resume

        /// <summary>
        /// The allies in the deploy bar for this level: the squad picked before the level, or the level's roster
        /// (the starters). The first time a player starts Level 1 the starters are used, so the tutorial matches.
        /// </summary>
        public UnitData[] Squad { get; private set; } = new UnitData[0];

        private UnitData[] ChooseSquad(int levelNumber, string savedSquad)
        {
            UnitData[] squad = savedSquad != null
                ? PlayerSquad.Decode(savedSquad, Stage)
                : GameSession.Squad;
            bool tutorial = levelNumber == 1 && savedSquad == null && TutorialController.IsDue(PlayerName);
            if (tutorial || squad == null || squad.Length == 0) return PlayerSquad.Default(Level);
            return squad;
        }

        public void StartLevel(int levelNumber)
        {
            LevelNumber = levelNumber;
            Level = Stage.GetLevel(levelNumber);
            Squad = ChooseSquad(levelNumber, null);
            PrepareBoard();
            _temperature.Init(Level.startingTemperature, Level.lossThreshold);
            _waves.Begin(Level);
            _waves.SpawnNextWave();
            Current = State.Playing;
            ApplySpeed();
            _ui.ShowHud();
            _turns.BeginPlayerTurn(Level);   // also autosaves
            BeginOpening(false, $"Level {LevelNumber} · {Level.locationName}. Wave 1 of {_waves.TotalWaves}: keep it below {Level.lossThreshold:0.0}°C, {PlayerName}!", UIManager.Info);
        }

        // ---------------------------------------------------------------- level opening

        /// <summary>Starts the opening sequence (intro card, greeting, climate facts, Level 1 tutorial), replacing a running one.</summary>
        private void BeginOpening(bool resumed, string greeting, Color tone)
        {
            if (_opening != null) StopCoroutine(_opening);
            if (_tutorial != null) _tutorial.Stop();
            if (_intro != null) _intro.Stop();
            _opening = StartCoroutine(LevelOpening(resumed, greeting, tone));
            _ui.RefreshHud();   // the intro has started: End Turn and the cards show as unavailable
        }

        /// <summary>
        /// 1. the intro (fade from black, camera glide, title card), 2. the greeting line, 3. the climate facts of this
        /// moment, 4. the tutorial the first time a player starts Level 1. The player cannot act during 1 and 3.
        /// </summary>
        private IEnumerator LevelOpening(bool resumed, string greeting, Color tone)
        {
            int wave = _waves.WaveNumber;
            if (_intro != null)
            {
                string resumeTag = resumed ? $"WELCOME BACK · WAVE {wave} · TURN {_waves.TurnInWave}" : null;
                yield return _intro.Play(Stage, Level, LevelNumber, resumeTag, _camera);
            }
            _ui.RefreshHud();
            _ui.Announce(greeting, tone);
            if (_popups != null)
            {
                if (resumed) yield return ShowMissedFacts(wave);
                else yield return _popups.ShowFor(Stage, LevelNumber, PopupTrigger.LevelStart, 1);
            }
            if (!resumed && LevelNumber == 1 && _tutorial != null && Current == State.Playing && TutorialController.IsDue(PlayerName))
                yield return _tutorial.Run(false);
            _opening = null;
        }

        /// <summary>Pause ▸ HOW TO PLAY: back to the game, then the tutorial from the start.</summary>
        public void ReplayTutorial()
        {
            if (_tutorial == null) return;
            Resume();
            if (!TutorialController.IsRunning) _tutorial.Begin(true);
        }

        /// <summary>After Continue: facts of this level that were on screen when the app closed (start, or a cleared wave).</summary>
        private IEnumerator ShowMissedFacts(int wave)
        {
            yield return _popups.ShowFor(Stage, LevelNumber, PopupTrigger.LevelStart, 1);
            for (int w = 1; w < wave; w++) yield return _popups.ShowFor(Stage, LevelNumber, PopupTrigger.AfterWave, w);
        }

        /// <summary>Rebuilds the board from a saved game. Returns false if the save does not fit this stage.</summary>
        private bool ResumeLevel(SessionSnapshot s)
        {
            LevelData level = Stage.stageId == s.stageId ? Stage.GetLevel(s.levelNumber) : null;
            if (level == null || s.waveNumber < 1 || s.waveNumber > level.waves.Length)
            {
                _database.DeleteSession(PlayerName);
                return false;
            }

            LevelNumber = s.levelNumber;
            Level = level;
            Squad = ChooseSquad(s.levelNumber, s.squad ?? "");
            PrepareBoard();
            _temperature.Init(Level.startingTemperature, Level.lossThreshold);
            _temperature.Restore(s.temperature, s.peakTemperature);
            _waves.Restore(Level, s.waveNumber, s.turnNumber);

            foreach (SavedUnit u in s.allies)
            {
                UnitData data = Stage.FindUnit(u.unitId);
                if (data == null || !_grid.IsAllySlotFree(u.lane)) continue;
                UnitBase unit = _factory.Spawn(data, u.lane, data.maxHp);
                unit.SetHp(u.hp);
                unit.RestoreStatus(u.status);
                _grid.Place(unit, u.lane);
            }
            foreach (SavedUnit u in s.enemies)
            {
                UnitData data = Stage.FindUnit(u.unitId);
                if (data == null) continue;
                if (data is BossData bossData)
                {
                    BossUnit boss = _factory.SpawnBoss(bossData, Level.ScaledHp(bossData));
                    boss.RestoreTurn(s.bossTurn);
                    boss.SetHp(u.hp);
                    _grid.PlaceBoss(boss);
                    break;
                }
                UnitBase unit = _factory.Spawn(data, u.lane, Level.ScaledHp(data));
                unit.SetHp(u.hp);
                unit.RestoreStatus(u.status);
                _grid.Place(unit, u.lane);
            }

            _oxygen.Restore(s.oxygenLeft, s.oxygenAllowance, s.oxygenPenalty);
            Current = State.Playing;
            ApplySpeed();
            _ui.ShowHud();
            _turns.ResumePlayerTurn();
            BeginOpening(true, $"Welcome back, {PlayerName}! Level {LevelNumber}, Wave {s.waveNumber} of {_waves.TotalWaves}, Turn {s.turnNumber}.", UIManager.Good);
            return true;
        }

        private void PrepareBoard()
        {
            _grid.ClearAll();
            _lanes.Init(Level);
            if (Squad.Length > 0) _deploy.Select(Squad[0]);
        }

        // ---------------------------------------------------------------- saving

        /// <summary>Saves the board to saved_session. Skipped while a turn is resolving (the last save stands).</summary>
        public void Autosave()
        {
            if (Current != State.Playing && Current != State.Paused) return;
            if (_turns.IsResolving || _waves.CurrentWave == null) return;

            var s = new SessionSnapshot
            {
                playerName = PlayerName,
                stageId = Stage.stageId,
                levelNumber = LevelNumber,
                waveNumber = _waves.WaveNumber,
                turnNumber = _waves.TurnInWave,
                oxygenLeft = _oxygen.Current,
                oxygenAllowance = _oxygen.Allowance,
                oxygenPenalty = _oxygen.Penalty,
                temperature = _temperature.Current,
                bossTurn = _grid.Boss != null ? _grid.Boss.BossTurn : 0,
                peakTemperature = _temperature.Peak,
                squad = PlayerSquad.Encode(Squad),
            };
            foreach (UnitBase a in _grid.Allies)
                if (!a.IsDead) s.allies.Add(new SavedUnit { lane = a.Lane, unitId = a.Data.unitId, hp = a.Hp, maxHp = a.MaxHp, status = a.Status.Copy() });
            foreach (UnitBase e in _grid.Enemies)
                if (!e.IsDead) s.enemies.Add(new SavedUnit { lane = e.Lane, unitId = e.Data.unitId, hp = e.Hp, maxHp = e.MaxHp, status = e.Status.Copy() });
            LastSaveOk = _database.SaveSession(s);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Autosave();
        }

        private void OnApplicationQuit() => Autosave();

        // ---------------------------------------------------------------- end of level

        public void EndLevel(bool won)
        {
            if (Current != State.Playing) return;
            Current = State.Ended;
            Time.timeScale = 1f;
            if (_tutorial != null) _tutorial.Stop();
            _grid.SetDeployHighlights(false);
            if (_camera != null) _camera.SnapHome();
            int waves = won ? _waves.TotalWaves : _waves.WavesCleared;
            float final = _temperature.Current;

            LevelProgress before = FindProgress(LevelNumber);
            _database.DeleteSession(PlayerName);
            int stars = won ? StarRating.For(_temperature.Peak) : 0;
            _database.RecordAttempt(PlayerName, Stage.stageId, LevelNumber, won, final, stars);
            GameResult record = _database.Insert(PlayerName, won, final, waves);

            // Carbon Credits: first clear, boss bonus, new stars, or the smaller replay share. A loss pays nothing.
            CreditReward reward = default;
            int wallet = _database.GetCredits(PlayerName);
            if (won)
            {
                bool clearedBefore = before != null && before.IsCleared == 1;
                reward = CreditRules.ForWin(LevelNumber, Level.isBossLevel, clearedBefore, StarRating.For(before), stars);
                int balance = _database.AddCredits(PlayerName, reward.Total);
                if (balance >= 0) wallet = balance;
            }

            // Stars and stage totals, read back from level_progress after this attempt.
            int cleared = 0, stageStars = 0;
            LevelProgress after = null;
            foreach (LevelProgress p in _database.GetProgress(PlayerName, Stage.stageId))
            {
                if (p.IsCleared == 1) cleared++;
                stageStars += StarRating.For(p);
                if (p.LevelNumber == LevelNumber) after = p;
            }
            var summary = new UIManager.EndSummary
            {
                Won = won,
                Record = record,
                Threshold = Level.lossThreshold,
                CanGoNext = won && HasNextLevel,
                Stars = stars,
                Peak = _temperature.Peak,
                BestStars = StarRating.For(after),
                NewBest = won && before != null && before.IsCleared == 1 && stars > StarRating.For(before),
                StageComplete = won && !HasNextLevel && cleared >= Stage.LevelCount,
                StageCleared = cleared,
                StageLevels = Stage.LevelCount,
                StageStars = stageStars,
                Reward = reward,
                Wallet = wallet,
            };
            _ui.ShowEnd(summary);
        }

        private LevelProgress FindProgress(int level)
        {
            foreach (LevelProgress p in _database.GetProgress(PlayerName, Stage.stageId))
                if (p.LevelNumber == level) return p;
            return null;
        }

        public void Restart()
        {
            if (_camera != null) _camera.SnapHome();
            Time.timeScale = 1f;
            _database.DeleteSession(PlayerName);
            StartLevel(LevelNumber);
        }

        public void NextLevel()
        {
            if (!HasNextLevel) { ToLevelSelect(); return; }
            StartLevel(LevelNumber + 1);
        }

        // ---------------------------------------------------------------- pause and leaving

        public void Pause()
        {
            if (Current != State.Playing || PopupManager.IsOpen || LevelIntro.IsPlaying) return;
            Current = State.Paused;
            Time.timeScale = 0f;
            Autosave();
            _ui.ShowPause(true);
        }

        public void Resume()
        {
            if (Current != State.Paused) return;
            Current = State.Playing;
            ApplySpeed();
            _ui.ShowPause(false);
        }

        /// <summary>Back to the Main Menu title. The game is saved first, so Continue can resume it.</summary>
        public void ToMainMenu()
        {
            Autosave();
            GameSession.OpenLevelSelect = false;
            LoadMenu();
        }

        /// <summary>Back to the Baguio level path.</summary>
        public void ToLevelSelect()
        {
            Autosave();
            GameSession.OpenLevelSelect = true;
            LoadMenu();
        }

        /// <summary>Back to the stage cards.</summary>
        public void ToStageSelect()
        {
            Autosave();
            GameSession.OpenStageSelect = true;
            LoadMenu();
        }

        private void LoadMenu()
        {
            Time.timeScale = 1f;
            GameSession.PlayerName = PlayerName;
            GameSession.Stage = Stage;
            GameSession.LevelNumber = LevelNumber;
            SceneManager.LoadScene(GameSession.MenuScene);
        }

        public void ApplySpeed()
        {
            if (Current == State.Playing) Time.timeScale = SettingsStore.BattleSpeed;
        }

        private void OnTemperatureChanged(float value)
        {
            // The cold Baguio fog thins as the planet warms.
            if (_fog != null) _fog.SetDensity(Mathf.Lerp(0.85f, 0.15f, _temperature.Normalized));
        }
    }
}
