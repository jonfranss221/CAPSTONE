using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>Owns every screen of the battle scene: HUD, deploy bar, boss panel, pause, end screen and Results.</summary>
    public class UIManager : MonoBehaviour
    {
        public static readonly Color Info = new Color(0.85f, 0.95f, 1f);
        public static readonly Color Good = new Color(0.55f, 1f, 0.6f);
        public static readonly Color Warning = new Color(1f, 0.75f, 0.3f);

        [Header("HUD")]
        [SerializeField] private GameObject _hud;
        [SerializeField] private Text _levelTitle;
        [SerializeField] private Text _waveValue;
        [SerializeField] private Image _temperatureFill;
        [SerializeField] private Image _temperatureBack;
        [SerializeField] private Text _temperatureValue;
        [SerializeField] private Text _oxygenValue;
        [SerializeField] private Text _oxygenSub;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _endTurnButton;
        [SerializeField] private Text _hint;
        [SerializeField] private DeployCard[] _cards = new DeployCard[0];
        [SerializeField] private Text _announcement;
        [SerializeField] private RectTransform _floatingLayer;

        [Header("Boss panel")]
        [SerializeField] private GameObject _bossPanel;
        [SerializeField] private Text _bossName;
        [SerializeField] private Text _bossHp;
        [SerializeField] private Image _bossHpFill;
        [SerializeField] private Text _bossNext;
        [SerializeField] private Text _bossSlam;
        [SerializeField] private Text _bossNote;

        [Header("Pause")]
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private Text _pauseInfo;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _pauseLevelSelectButton;
        [SerializeField] private Button _pauseMenuButton;

        [Header("End screen")]
        [SerializeField] private GameObject _endPanel;
        [SerializeField] private Text _endTitle;
        [SerializeField] private Text _endBody;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _endLevelSelectButton;
        [SerializeField] private Button _endResultsButton;

        [Header("Results")]
        [SerializeField] private ResultsPanel _results;

        [Header("Systems")]
        [SerializeField] private TemperatureSystem _temperature;
        [SerializeField] private OxygenPointSystem _oxygen;
        [SerializeField] private WaveManager _waves;
        [SerializeField] private GridManager _grid;
        [SerializeField] private DeploySystem _deploy;
        [SerializeField] private TurnManager _turns;
        [SerializeField] private ResultsDatabase _database;
        [SerializeField] private Font _font;

        private Coroutine _announceRoutine;
        private Coroutine _flashRoutine;
        private BossUnit _trackedBoss;

        private static readonly Color HeatHex = new Color(1f, 0.6f, 0.53f);

        private void Awake()
        {
            _endTurnButton.onClick.AddListener(() => _turns.EndTurn());
            _pauseButton.onClick.AddListener(() => GameManager.Instance.Pause());
            _resumeButton.onClick.AddListener(() => GameManager.Instance.Resume());
            _restartButton.onClick.AddListener(() => { ShowPause(false); GameManager.Instance.Restart(); });
            _pauseLevelSelectButton.onClick.AddListener(() => GameManager.Instance.ToLevelSelect());
            _pauseMenuButton.onClick.AddListener(() => GameManager.Instance.ToMainMenu());
            _nextButton.onClick.AddListener(() => GameManager.Instance.NextLevel());
            _playAgainButton.onClick.AddListener(() => GameManager.Instance.Restart());
            _endLevelSelectButton.onClick.AddListener(() => GameManager.Instance.ToLevelSelect());
            _endResultsButton.onClick.AddListener(OpenResults);

            _temperature.Changed += _ => RefreshHud();
            _oxygen.Changed += RefreshHud;
            _deploy.SelectionChanged += _ => RefreshHud();
            _deploy.Deployed += _ => RefreshHud();

            _pausePanel.SetActive(false);
            _endPanel.SetActive(false);
            _bossPanel.SetActive(false);
        }

        // ---------------- screens ----------------

        public void ShowHud()
        {
            _endPanel.SetActive(false);
            _pausePanel.SetActive(false);
            _results.Hide();
            _hud.SetActive(true);
            GameManager gm = GameManager.Instance;
            UnitData[] roster = gm.Level.allyRoster;
            for (int i = 0; i < _cards.Length; i++)
                _cards[i].Setup(i < roster.Length ? roster[i] : null, unit => _deploy.Select(unit));
            _levelTitle.text = $"LEVEL {gm.LevelNumber} · {gm.Level.locationName.ToUpperInvariant()}";
            RefreshHud();
        }

        public void ShowPause(bool show)
        {
            _pausePanel.SetActive(show);
            if (!show) return;
            _pausePanel.transform.SetAsLastSibling();
            GameManager gm = GameManager.Instance;
            string saved = gm.LastSaveOk ? "Progress saved" : "Progress could not be saved";
            _pauseInfo.text = $"{gm.PlayerName} · Baguio Level {gm.LevelNumber} · Wave {_waves.WaveNumber} of {_waves.TotalWaves} · Turn {_waves.TurnInWave}\n{saved}. Continue from the Main Menu returns here.";
        }

        public void ShowEnd(bool won, GameResult record, float threshold, bool canGoNext)
        {
            RefreshHud();
            _bossPanel.SetActive(false);
            _endPanel.SetActive(true);
            _endPanel.transform.SetAsLastSibling();
            _nextButton.gameObject.SetActive(canGoNext);
            // With no next level, Play Again moves to the centre of the dialog.
            var again = (RectTransform)_playAgainButton.transform;
            again.anchoredPosition = new Vector2(canGoNext ? 360f : 200f, again.anchoredPosition.y);
            GameManager gm = GameManager.Instance;
            _endTitle.text = won ? "VICTORY" : "DEFEAT";
            _endTitle.color = won ? new Color(0.89f, 0.68f, 0.17f) : new Color(1f, 0.45f, 0.38f);
            string where = $"Level {gm.LevelNumber} · {gm.Level.locationName}";
            _endBody.text = won
                ? $"{where} is safe, {record.player_name}!\nAll {record.waves_cleared} waves cleared at {record.final_temperature:0.00} °C, below the {threshold:0.0} °C limit."
                  + (canGoNext ? $"\nLevel {gm.LevelNumber + 1} is now unlocked." : (gm.Level.isBossLevel ? $"\nYou defeated the {BossName(gm)}, the Baguio boss!" : ""))
                : $"{where}: the temperature reached {record.final_temperature:0.00} °C, crossing the {threshold:0.0} °C limit.\nWaves cleared: {record.waves_cleared} of {_waves.TotalWaves}.";
            _endBody.text += record.id > 0
                ? $"\n\nResult saved · {record.created_at}"
                : "\n\nResult not saved: the database could not be written.";
        }

        private static string BossName(GameManager gm)
        {
            foreach (WaveData w in gm.Level.waves)
                foreach (UnitData e in w.enemies)
                    if (e is BossData) return e.displayName;
            return "boss";
        }

        public void OpenResults() => _results.Show(_database.GetAll(), _database.FilePath);

        // ---------------- HUD ----------------

        public void RefreshHud()
        {
            if (_temperature == null || _waves.CurrentWave == null || GameManager.Instance == null) return;

            float n = _temperature.Normalized;
            _temperatureFill.fillAmount = Mathf.Max(0.02f, n);
            _temperatureFill.color = n < 0.5f
                ? Color.Lerp(new Color(0.3f, 0.75f, 0.45f), new Color(0.89f, 0.68f, 0.17f), n * 2f)
                : Color.Lerp(new Color(0.89f, 0.68f, 0.17f), new Color(0.85f, 0.33f, 0.18f), (n - 0.5f) * 2f);
            string hot = ColorUtility.ToHtmlStringRGB(n < 0.5f ? new Color(0.95f, 0.92f, 0.84f) : HeatHex);
            _temperatureValue.text = $"<b><color=#{hot}>{_temperature.Current:0.00} °C</color></b> / limit {_temperature.Threshold:0.0} °C";

            _oxygenValue.text = $"{_oxygen.Current} / {_oxygen.Allowance}";
            _oxygenSub.text = _waves.CurrentWave.ignorePenalty ? "Oxygen · no penalty"
                : _oxygen.Penalty > 0 ? $"Oxygen · −{_oxygen.Penalty} penalty" : "Oxygen";

            bool boss = _grid.Boss != null;
            _waveValue.text = $"WAVE {_waves.WaveNumber} / {_waves.TotalWaves}{(boss ? " · BOSS" : "")} · TURN {_waves.TurnInWave}";

            GameManager gm = GameManager.Instance;
            bool canAct = gm.CanPlayerAct;
            _endTurnButton.interactable = canAct;
            _pauseButton.interactable = gm.Current == GameManager.State.Playing && !_turns.IsResolving;
            foreach (DeployCard card in _cards)
            {
                if (card.Data == null) continue;
                card.Refresh(card.Data == _deploy.Selected, _oxygen.CanAfford(card.Data.opCost), canAct);
            }
            RefreshBoss();
            RefreshHint();
        }

        private void RefreshHint()
        {
            UnitData sel = _deploy.Selected;
            BossUnit boss = _grid.Boss;
            if (boss != null && !boss.IsDead)
            {
                BossUnit.BossMove move = boss.PlanNext(_grid);
                var empty = new System.Collections.Generic.List<int>();
                foreach (int lane in move.Lanes) if (_grid.IsAllySlotFree(lane)) empty.Add(lane + 1);
                string lanes = empty.Count == 0 ? "" : (empty.Count == 1 ? $"Fill Lane {empty[0]} before ending the turn. " : $"Fill Lanes {string.Join(", ", empty)} before ending the turn. ");
                _hint.text = lanes + (move.IsSlam ? "The slam hits every lane." : "Cleave targets the lane of your strongest unit.");
                return;
            }
            _hint.text = sel == null
                ? "Tap a unit card, then a glowing slot on the road. Tap END TURN when ready."
                : $"<b>{sel.displayName}</b>: {sel.opCost} OP · {sel.maxHp} HP · {sel.damage} damage. Tap a glowing slot to deploy.";
        }

        private void RefreshBoss()
        {
            BossUnit boss = _grid.Boss;
            if (boss != _trackedBoss)
            {
                if (_trackedBoss != null) { _trackedBoss.Damaged -= OnBossDamaged; _trackedBoss.Enraged -= OnBossEnraged; }
                _trackedBoss = boss;
                if (boss != null) { boss.Damaged += OnBossDamaged; boss.Enraged += OnBossEnraged; }
            }
            _grid.SetDangerLanes(null);
            bool show = boss != null && !boss.IsDead && GameManager.Instance.Current != GameManager.State.Ended;
            _bossPanel.SetActive(show);
            if (!show) return;

            BossData data = boss.Boss;
            _bossName.text = $"{data.displayName.ToUpperInvariant()} <size=14><color=#E7A89F>· Baguio boss</color></size>";
            _bossHp.text = $"{boss.Hp} / {boss.MaxHp}";
            _bossHpFill.fillAmount = boss.Hp / (float)boss.MaxHp;

            BossUnit.BossMove move = boss.PlanNext(_grid);
            if (move.IsSlam)
            {
                _bossNext.text = $"<color=#F3C95A><b>Next: LANDSLIDE SLAM</b></color> all 6 lanes, {move.Damage} damage";
                _bossSlam.text = "Every empty lane adds <b>+0.10 °C</b>. Fill all six!";
            }
            else
            {
                _bossNext.text = $"<color=#FF9A86><b>Next: CLEAVE</b></color> lanes {move.Lanes[0] + 1}–{move.Lanes[move.Lanes.Count - 1] + 1}, {move.Damage} damage each";
                int t = boss.TurnsUntilSlam;
                _bossSlam.text = $"<color=#F3C95A><b>LANDSLIDE SLAM</b></color> all 6 lanes, {data.slamDamage} damage, in <b>{t} turn{(t == 1 ? "" : "s")}</b>";
            }
            _bossNote.text = boss.IsEnraged
                ? $"ENRAGED: cleave {data.enragedCleaveDamage}, slam every {data.enragedSlamEvery} turns"
                : $"Gold tick = enrage at {Mathf.RoundToInt(data.enrageAtFraction * 100)}% HP: cleave {data.enragedCleaveDamage}, slam every {data.enragedSlamEvery} turns";
            if (GameManager.Instance.CanPlayerAct) _grid.SetDangerLanes(move.Lanes);
        }

        public void ShowBossMove(BossUnit.BossMove move, BossUnit boss)
        {
            _grid.SetDangerLanes(move.Lanes);
            Announce(move.IsSlam ? $"{boss.Data.displayName}: LANDSLIDE SLAM!" : $"{boss.Data.displayName}: CLEAVE!", new Color(1f, 0.45f, 0.38f));
        }

        private void OnBossDamaged(UnitBase _) => RefreshBoss();

        private void OnBossEnraged(BossUnit boss)
            => Announce($"The {boss.Data.displayName} is ENRAGED! Harder cleaves and more slams.", new Color(1f, 0.45f, 0.38f));

        public void Announce(string message, Color color)
        {
            if (_announceRoutine != null) StopCoroutine(_announceRoutine);
            _announceRoutine = StartCoroutine(AnnounceRoutine(message, color));
        }

        private IEnumerator AnnounceRoutine(string message, Color color)
        {
            _announcement.text = message;
            _announcement.gameObject.SetActive(true);
            for (float t = 0f; t < 3.2f; t += Time.unscaledDeltaTime)
            {
                Color c = color;
                c.a = t < 2.6f ? 1f : 1f - (t - 2.6f) / 0.6f;
                _announcement.color = c;
                yield return null;
            }
            _announcement.gameObject.SetActive(false);
        }

        /// <summary>Damage numbers and °C changes that float up from a unit (Settings can hide them).</summary>
        public void ShowFloatingText(Vector3 worldPos, string text, Color color)
        {
            if (Camera.main == null || !SettingsStore.ShowNumbers) return;
            StartCoroutine(FloatRoutine(worldPos, text, color));
        }

        private IEnumerator FloatRoutine(Vector3 worldPos, string value, Color color)
        {
            var go = new GameObject("FloatingText", typeof(RectTransform));
            go.transform.SetParent(_floatingLayer, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(160f, 36f);

            Vector3 start = Camera.main.WorldToScreenPoint(worldPos);
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                rt.position = start + Vector3.up * (50f * t * rt.lossyScale.y * 1.5f);
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
            Color normal = new Color(0.13f, 0.2f, 0.16f, 1f);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                _temperatureBack.color = Color.Lerp(new Color(0.8f, 0.15f, 0.1f, 1f), normal, t / 0.6f);
                yield return null;
            }
            _temperatureBack.color = normal;
        }
    }
}
