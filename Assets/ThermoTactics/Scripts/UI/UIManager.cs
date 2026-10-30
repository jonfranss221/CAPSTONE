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
        [Tooltip("Shown while a unit placed this turn is picked up: removes it with a full OP refund.")]
        [SerializeField] private Button _takeBackButton;
        [SerializeField] private Text _takeBackLabel;
        [Header("Carbon Credits (end screen)")]
        [SerializeField] private GameObject _endCredits;
        [SerializeField] private Text _endCreditsText;
        [SerializeField] private Text _endWalletText;
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
        [SerializeField] private Button _pauseTutorialButton;

        [Header("Lane caption (shown while End Turn resolves)")]
        [SerializeField] private GameObject _laneCaption;
        [SerializeField] private Text _laneTitle;
        [SerializeField] private Text _laneDetail;
        [SerializeField] private Image _laneAccent;

        [Header("End screen (level complete / failed)")]
        [SerializeField] private GameObject _endPanel;
        [SerializeField] private Image _endBorder;
        [SerializeField] private Text _endTitle;
        [SerializeField] private Text _endBody;
        [SerializeField] private Image[] _endStars = new Image[3];
        [SerializeField] private Text _endStarHint;
        [SerializeField] private GameObject _endNewBest;
        [SerializeField] private Button _endPrimary;
        [SerializeField] private Text _endPrimaryLabel;
        [SerializeField] private Image _endPrimaryIcon;
        [SerializeField] private Button[] _endSecondary = new Button[3];
        [SerializeField] private Text[] _endSecondaryLabels = new Text[3];
        [SerializeField] private Image[] _endSecondaryIcons = new Image[3];
        [SerializeField] private Button _endResultsButton;

        [Header("Stage complete (after the stage's last level)")]
        [SerializeField] private GameObject _stagePanel;
        [SerializeField] private Text _stageTitle;
        [SerializeField] private Text _stageStats;
        [SerializeField] private Text _stageStarCount;
        [SerializeField] private Image _stageStarFill;
        [SerializeField] private Image[] _stageLevelStars = new Image[3];
        [SerializeField] private Text _stageNext;
        [SerializeField] private Button _stageSelectButton;
        [SerializeField] private Button _stageLevelSelectButton;
        [SerializeField] private Button _stageRetryButton;
        [SerializeField] private Button _stageResultsButton;

        [Header("Icons")]
        [SerializeField] private Sprite _iconNext;
        [SerializeField] private Sprite _iconRetry;
        [SerializeField] private Sprite _iconLevels;
        [SerializeField] private Sprite _iconStages;
        [SerializeField] private Sprite _iconResults;

        [Header("Results")]
        [SerializeField] private ResultsPanel _results;

        [Header("Post-quiz (after the stage's last level)")]
        [SerializeField] private QuizPanel _quiz;

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
            if (_pauseTutorialButton != null) _pauseTutorialButton.onClick.AddListener(() => GameManager.Instance.ReplayTutorial());
            _endPrimary.onClick.AddListener(() => _primaryAction?.Invoke());
            for (int i = 0; i < _endSecondary.Length; i++)
            {
                int index = i;
                _endSecondary[i].onClick.AddListener(() => _secondaryActions[index]?.Invoke());
            }
            _endResultsButton.onClick.AddListener(OpenResults);
            _stageSelectButton.onClick.AddListener(() => GameManager.Instance.ToStageSelect());
            _stageLevelSelectButton.onClick.AddListener(() => GameManager.Instance.ToLevelSelect());
            _stageRetryButton.onClick.AddListener(() => GameManager.Instance.Restart());
            _stageResultsButton.onClick.AddListener(OpenResults);

            _temperature.Changed += _ => RefreshHud();
            _oxygen.Changed += RefreshHud;
            _deploy.SelectionChanged += _ => RefreshHud();
            _deploy.Deployed += _ => RefreshHud();
            _deploy.Rearranged += RefreshHud;
            _deploy.BoardSelectionChanged += _ => RefreshHud();
            if (_takeBackButton != null) _takeBackButton.onClick.AddListener(() => _deploy.TakeBack());

            _pausePanel.SetActive(false);
            _endPanel.SetActive(false);
            _stagePanel.SetActive(false);
            _bossPanel.SetActive(false);
            _laneCaption.SetActive(false);
        }

        // ---------------- screens ----------------

        public void ShowHud()
        {
            _endPanel.SetActive(false);
            _stagePanel.SetActive(false);
            _laneCaption.SetActive(false);
            _pausePanel.SetActive(false);
            _results.Hide();            _hud.SetActive(true);
            GameManager gm = GameManager.Instance;
            UnitData[] roster = gm.Squad;
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

        /// <summary>Everything the end screens need about the level that just ended.</summary>
        public class EndSummary
        {
            public bool Won;
            public GameResult Record;
            public float Threshold;
            public bool CanGoNext;
            public int Stars;            // stars earned this time (0 on a loss)
            public float Peak;           // highest temperature reached in the level
            public int BestStars;        // best stars ever on this level
            public bool NewBest;         // lower winning temperature than before
            public bool StageComplete;   // the stage's last level was won
            public int StageCleared;     // levels cleared in the stage
            public int StageLevels;
            public int StageStars;       // stars in the whole stage
            public CreditReward Reward;  // Carbon Credits earned by this win (Total 0 on a loss)
            public int Wallet;           // the player's balance after the reward
        }

        private System.Action _primaryAction;
        private readonly System.Action[] _secondaryActions = new System.Action[3];

        public void ShowEnd(EndSummary e)
        {
            RefreshHud();
            _bossPanel.SetActive(false);
            _laneCaption.SetActive(false);
            GameManager gm = GameManager.Instance;
            if (e.StageComplete)
            {
                // The post-quiz comes first, once per player; the Stage Complete screen then shows its result.
                if (_quiz != null && QuizManager.NeedsPost(_database, gm.PlayerName, gm.Stage))
                    _quiz.Show(gm.Stage, QuizManager.Post, gm.PlayerName, _database, o => ShowStageComplete(e, gm, o));
                else
                    ShowStageComplete(e, gm, QuizManager.LoadPost(_database, gm.PlayerName, gm.Stage));
                return;
            }

            _endPanel.SetActive(true);
            _endPanel.transform.SetAsLastSibling();
            Color gold = new Color(0.89f, 0.68f, 0.17f), red = new Color(1f, 0.45f, 0.38f);
            _endTitle.text = e.Won ? "LEVEL COMPLETE" : "LEVEL FAILED";
            _endTitle.color = e.Won ? gold : red;
            _endBorder.color = e.Won ? gold : new Color(0.64f, 0.14f, 0.11f);
            GameResult r = e.Record;
            string where = $"Level {gm.LevelNumber} · {gm.Level.locationName}";
            _endBody.text = e.Won
                ? $"{where} is safe, {r.player_name}!\nAll {r.waves_cleared} waves cleared. Highest temperature: <b>{e.Peak:0.00} °C</b> (limit {e.Threshold:0.0} °C)."
                  + (e.CanGoNext ? $"\n<color=#F3C95A>Level {gm.LevelNumber + 1} is now unlocked.</color>" : "")
                : $"{where}: the temperature reached <b>{r.final_temperature:0.00} °C</b> and crossed the {e.Threshold:0.0} °C limit.\nWaves cleared: {r.waves_cleared} of {_waves.TotalWaves}. Fill more lanes so enemies can't heat the planet.";
            _endBody.text += r.id > 0 ? $"\n<size=13><color=#CFC6B2>Result saved · {r.created_at}</color></size>"
                                      : "\n<size=13><color=#FF9A86>Result not saved: the database could not be written.</color></size>";

            ShowCredits(e);
            foreach (Image star in _endStars) star.gameObject.SetActive(e.Won);
            _endNewBest.SetActive(e.Won && e.NewBest);
            _endStarHint.text = e.Won
                ? (e.Stars < e.BestStars ? $"Your best on this level: {e.BestStars} stars. " : "") + StarRating.NextStarHint(e.Stars)
                : $"Win to earn stars. Keep the temperature at {StarRating.TwoStarAt:0.00} °C or lower for 2, {StarRating.ThreeStarAt:0.00} °C for 3.";
            if (e.Won) StartCoroutine(PopStars(_endStars, e.Stars));

            if (e.Won && e.CanGoNext)
            {
                SetPrimary("NEXT LEVEL", _iconNext, gold, () => gm.NextLevel());
                SetSecondary(0, "RETRY", _iconRetry, () => gm.Restart());
                SetSecondary(1, "LEVEL SELECT", _iconLevels, () => gm.ToLevelSelect());
                SetSecondary(2, "STAGE SELECT", _iconStages, () => gm.ToStageSelect());
            }
            else if (e.Won)
            {
                SetPrimary("LEVEL SELECT", _iconLevels, gold, () => gm.ToLevelSelect());
                SetSecondary(0, "RETRY", _iconRetry, () => gm.Restart());
                SetSecondary(1, "STAGE SELECT", _iconStages, () => gm.ToStageSelect());
                SetSecondary(2, "RESULTS", _iconResults, OpenResults);
            }
            else
            {
                SetPrimary("RETRY LEVEL", _iconRetry, new Color(0.64f, 0.14f, 0.11f), () => gm.Restart());
                SetSecondary(0, "LEVEL SELECT", _iconLevels, () => gm.ToLevelSelect());
                SetSecondary(1, "STAGE SELECT", _iconStages, () => gm.ToStageSelect());
                SetSecondary(2, "RESULTS", _iconResults, OpenResults);
            }
            _endResultsButton.gameObject.SetActive(e.Won && e.CanGoNext);
        }

        /// <summary>The "+120 CC" chip counts up from 0; the wallet line shows what the reward was made of.</summary>
        private void ShowCredits(EndSummary e)
        {
            if (_endCredits == null) return;
            bool paid = e.Won && e.Reward.Total > 0;
            _endCredits.SetActive(paid);
            if (_endWalletText != null)
            {
                _endWalletText.gameObject.SetActive(e.Won);
                if (e.Won)
                {
                    var parts = new System.Collections.Generic.List<string>();
                    parts.Add(e.Reward.Replay ? $"replay {e.Reward.Base}" : $"first clear {e.Reward.Base}");
                    if (e.Reward.BossBonus > 0) parts.Add($"boss {e.Reward.BossBonus}");
                    if (e.Reward.StarBonus > 0) parts.Add($"new stars {e.Reward.StarBonus}");
                    _endWalletText.text = $"{string.Join(" + ", parts)} · Wallet {CreditRules.Format(e.Wallet)}";
                }
            }
            if (paid) StartCoroutine(CountUp(_endCreditsText, e.Reward.Total));
        }

        private static IEnumerator CountUp(Text label, int total)
        {
            label.text = "+0 CC";
            yield return new WaitForSecondsRealtime(0.35f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.9f)
            {
                label.text = $"+{Mathf.RoundToInt(Mathf.SmoothStep(0f, total, t))} CC";
                yield return null;
            }
            label.text = "+" + CreditRules.Format(total);
        }

        private void SetPrimary(string label, Sprite icon, Color fill, System.Action action)
        {
            _primaryAction = action;
            _endPrimaryLabel.text = label;
            bool dark = fill.r + fill.g + fill.b > 1.5f;   // gold → dark text; red → light text
            _endPrimaryLabel.color = dark ? new Color(0.102f, 0.078f, 0.039f) : new Color(1f, 0.965f, 0.9f);
            _endPrimary.GetComponent<Image>().color = fill;
            if (_endPrimaryIcon != null) { _endPrimaryIcon.sprite = icon; _endPrimaryIcon.color = _endPrimaryLabel.color; }
        }

        private void SetSecondary(int i, string label, Sprite icon, System.Action action)
        {
            _secondaryActions[i] = action;
            _endSecondaryLabels[i].text = label;
            if (_endSecondaryIcons[i] != null) _endSecondaryIcons[i].sprite = icon;
        }

        private void ShowStageComplete(EndSummary e, GameManager gm, QuizOutcome quiz)
        {
            _stagePanel.SetActive(true);
            _stagePanel.transform.SetAsLastSibling();
            StageData stage = gm.Stage;
            int maxStars = e.StageLevels * StarRating.MaxStars;
            _stageTitle.text = $"{stage.stageName.ToUpperInvariant()} CLEARED";
            _stageStats.text =
                $"You defeated the <b>{BossName(gm)}</b>. Highest temperature: {e.Peak:0.00} °C.\n" +
                $"Levels cleared: <b>{e.StageCleared} / {e.StageLevels}</b>\n" +
                $"Stage {stage.stageId} saved by <b>{e.Record.player_name}</b>." +
                (e.Reward.Total > 0 ? $" <color=#8FE09B>+{CreditRules.Format(e.Reward.Total)}</color> · Wallet {CreditRules.Format(e.Wallet)}" : "") +
                (e.Record.id > 0 ? "" : "\n<color=#FF9A86>Result not saved: the database could not be written.</color>") +
                // One line only: the stats box sits right above the STAGE STARS label.
                (quiz == null ? "" : $"\n<color=#F3C95A>Post-quiz {quiz.KeptScore} / {quiz.MaxScore} · knowledge gain {QuizManager.GainText(quiz.Gain)}</color>");
            _stageStarCount.text = $"{e.StageStars} / {maxStars}";
            _stageStarFill.fillAmount = maxStars > 0 ? e.StageStars / (float)maxStars : 0f;
            _stageNext.text = e.StageStars >= maxStars
                ? "Every star in the stage! <b>Stage 2 · Sierra Madre & Mangrove Coast</b> is unlocked (coming soon)."
                : $"<b>Stage 2 · Sierra Madre & Mangrove Coast</b> is unlocked (coming soon). Replay levels for the {maxStars - e.StageStars} missing stars.";
            StartCoroutine(PopStars(_stageLevelStars, e.Stars));
        }

        /// <summary>Stars pop in one by one: earned ones gold, the rest dim outlines.</summary>
        private IEnumerator PopStars(Image[] stars, int earned)
        {
            Color gold = new Color(0.95f, 0.75f, 0.2f), dim = new Color(0.29f, 0.35f, 0.31f);
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].color = i < earned ? gold : dim;
                stars[i].transform.localScale = Vector3.zero;
            }
            yield return new WaitForSecondsRealtime(0.25f);
            for (int i = 0; i < stars.Length; i++)
            {
                Transform t = stars[i].transform;
                for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.22f)
                {
                    float s = k < 0.7f ? Mathf.Lerp(0f, 1.25f, k / 0.7f) : Mathf.Lerp(1.25f, 1f, (k - 0.7f) / 0.3f);
                    t.localScale = Vector3.one * s;
                    yield return null;
                }
                t.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(0.08f);
            }
        }

        // ---------------- lane caption ----------------

        /// <summary>The banner under the header while End Turn resolves: "LANE 3" and what happened there.</summary>
        public void ShowLaneCaption(string title, string detail, Color tone)
        {
            _laneCaption.SetActive(true);
            // In the boss wave the boss panel sits at the top left, so the banner moves right of it.
            var rt = (RectTransform)_laneCaption.transform;
            bool boss = _bossPanel.activeSelf;
            rt.anchoredPosition = new Vector2(boss ? 230f : 0f, rt.anchoredPosition.y);
            rt.sizeDelta = new Vector2(boss ? 700f : 760f, rt.sizeDelta.y);
            _laneTitle.text = title;
            _laneDetail.text = detail;
            if (_laneAccent != null) _laneAccent.color = tone;
        }

        public void HideLaneCaption() => _laneCaption.SetActive(false);

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
            _endTurnButton.interactable = canAct && TutorialController.AllowEndTurn;
            _pauseButton.interactable = gm.Current == GameManager.State.Playing && !_turns.IsResolving;
            UnitBase picked = _deploy.BoardSelected;
            bool swapping = picked != null && picked.DeployedThisTurn;
            foreach (DeployCard card in _cards)
            {
                if (card.Data == null) continue;
                // While a unit placed this turn is picked up, a card is affordable if its refund covers the difference.
                int budget = _oxygen.Current + (swapping ? picked.Data.opCost : 0);
                card.Refresh(picked == null && card.Data == _deploy.Selected, budget >= card.Data.opCost, canAct);
            }
            if (_takeBackButton != null)
            {
                _takeBackButton.gameObject.SetActive(swapping && canAct);
                if (swapping && _takeBackLabel != null) _takeBackLabel.text = $"TAKE BACK +{picked.Data.opCost}";
                // The hint gives way to the button while it shows.
                _hint.rectTransform.offsetMax = new Vector2(swapping && canAct ? -410f : -250f, _hint.rectTransform.offsetMax.y);
            }
            RefreshBoss();
            RefreshHint();
        }

        private void RefreshHint()
        {
            UnitBase picked = _deploy.BoardSelected;
            if (picked != null)
            {
                string name = $"<b>{picked.Data.displayName}</b> · Lane {picked.Lane + 1}";
                _hint.text = picked.DeployedThisTurn
                    ? $"{name}, placed this turn. Tap an empty slot to move it (free), tap another card to swap it, or TAKE BACK. Locked at END TURN."
                    : !_oxygen.IsUntouched
                        ? $"{name}. Units from earlier turns can move only before you spend any Oxygen this turn. Tap it again to put it down."
                        : $"{name}. Tap an empty slot to move it there. This uses all your Oxygen ({_oxygen.Current} OP). Tap it again to cancel.";
                return;
            }
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
                ? "Tap a unit card, then a glowing slot on the road. Tap a placed unit to move it. Tap END TURN when ready."
                : UnitHint(sel);
        }

        /// <summary>Two lines for the deploy bar: stats, Speed and category, then the trait and what the category counters.</summary>
        public static string UnitHint(UnitData u)
        {
            string line1 = $"<b>{u.displayName}</b> · {UnitCategoryInfo.Label(u.category)} · {u.opCost} OP · {u.maxHp} HP · {u.damage} dmg · Speed {u.speed}";
            UnitCategory? counter = UnitCategoryInfo.Counters(u.category);
            string strong = counter.HasValue ? $" Strong vs {UnitCategoryInfo.Label(counter.Value)} (×1.5)." : "";
            string line2 = string.IsNullOrEmpty(u.traitText) ? "Tap a glowing slot to deploy." : u.traitText;
            return $"{line1}\n{line2}{strong}";
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

            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                if (Camera.main == null) break;
                // Recomputed every frame, so the number stays on its unit while the lane camera moves.
                rt.position = Camera.main.WorldToScreenPoint(worldPos) + Vector3.up * (50f * t * rt.lossyScale.y * 1.5f);
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
