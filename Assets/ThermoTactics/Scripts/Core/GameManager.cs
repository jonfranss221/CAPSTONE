using UnityEngine;

namespace ThermoTactics
{
    /// <summary>Orchestrates the level: login → play → win/loss → results.</summary>
    public class GameManager : MonoBehaviour
    {
        public enum State { Menu, Playing, Ended }

        public static GameManager Instance { get; private set; }

        [SerializeField] private LevelData _level;
        [SerializeField] private TurnManager _turns;
        [SerializeField] private WaveManager _waves;
        [SerializeField] private OxygenPointSystem _oxygen;
        [SerializeField] private TemperatureSystem _temperature;
        [SerializeField] private GridManager _grid;
        [SerializeField] private LaneController _lanes;
        [SerializeField] private DeploySystem _deploy;
        [SerializeField] private ResultsDatabase _database;
        [SerializeField] private UIManager _ui;
        [SerializeField] private FogScroller _fog;

        public State Current { get; private set; } = State.Menu;
        public LevelData Level => _level;
        public string PlayerName { get; private set; } = "";
        public bool CanPlayerAct => Current == State.Playing && !_turns.IsResolving;

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            _temperature.Changed += OnTemperatureChanged;
            _ui.ShowLogin();
        }

        public void StartLevel(string playerName)
        {
            PlayerName = ResultsDatabase.Sanitize(playerName);
            _grid.ClearAll();
            _lanes.Init(_level);
            _temperature.Init(_level.startingTemperature, _level.lossThreshold);
            _waves.Begin(_level);
            _waves.SpawnNextWave();
            Current = State.Playing;
            if (_level.allyRoster.Length > 0) _deploy.Select(_level.allyRoster[0]);
            _ui.ShowHud();
            _turns.BeginPlayerTurn(_level);
            _ui.Announce($"Wave 1 of {_waves.TotalWaves}: defend Kennon Road, {PlayerName}! Keep it below {_level.lossThreshold:0.0}°C.", UIManager.Info);
        }

        public void EndLevel(bool won)
        {
            if (Current != State.Playing) return;
            Current = State.Ended;
            _grid.SetDeployHighlights(false);
            int waves = won ? _waves.TotalWaves : _waves.WavesCleared;
            GameResult record = _database.Insert(PlayerName, won, _temperature.Current, waves);
            _ui.ShowEnd(won, record, _level.lossThreshold);
        }

        public void Restart() => StartLevel(PlayerName);

        public void BackToMenu()
        {
            _grid.ClearAll();
            Current = State.Menu;
            _ui.ShowLogin();
        }

        private void OnTemperatureChanged(float value)
        {
            // The cold Baguio fog thins as the planet warms.
            if (_fog != null) _fog.SetDensity(Mathf.Lerp(0.85f, 0.15f, _temperature.Normalized));
        }
    }
}
