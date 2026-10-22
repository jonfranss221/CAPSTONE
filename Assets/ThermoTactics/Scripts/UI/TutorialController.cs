using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// The Level 1 tutorial: a coach box and a pulsing gold frame point at each part of the screen while the player
    /// really picks a card, deploys a unit and ends a turn. It runs by itself the first time a player starts Level 1
    /// (remembered per player in SettingsStore), can be skipped at any step, and can be replayed from
    /// Pause ▸ HOW TO PLAY. While it runs, deploying and End Turn are allowed only at their own steps.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _coach;
        [SerializeField] private Text _stepLabel;
        [SerializeField] private Text _title;
        [SerializeField] private Text _body;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Text _nextLabel;
        [SerializeField] private Button _skipButton;
        [SerializeField] private RectTransform _highlight;

        [Header("HUD targets")]
        [SerializeField] private RectTransform _temperatureTarget;
        [SerializeField] private RectTransform _oxygenTarget;
        [SerializeField] private RectTransform _cardsTarget;
        [SerializeField] private RectTransform _endTurnTarget;

        [Header("Systems")]
        [SerializeField] private GridManager _grid;
        [SerializeField] private DeploySystem _deploy;
        [SerializeField] private TurnManager _turns;
        [SerializeField] private OxygenPointSystem _oxygen;
        [SerializeField] private UIManager _ui;

        private const int StepCount = 9;
        private const float CoachMargin = 20f, HeaderHeight = 84f, FooterHeight = 87f;

        /// <summary>True while the tutorial is on screen.</summary>
        public static bool IsRunning { get; private set; }

        /// <summary>Deploying is allowed (always true when no tutorial runs).</summary>
        public static bool AllowDeploy { get; private set; } = true;

        /// <summary>End Turn is allowed (always true when no tutorial runs).</summary>
        public static bool AllowEndTurn { get; private set; } = true;

#if UNITY_EDITOR
        /// <summary>Editor play-tests: the tutorial ends at once (and is marked done).</summary>
        public static bool DevSkip;
#endif

        public static bool IsDue(string player) => !SettingsStore.TutorialDone(player);

        private bool _next, _skip, _selected, _deployed;
        private Coroutine _routine;
        private RectTransform _rootRect;
        private Image _highlightImage;
        private Camera _cam;

        private void Awake()
        {
            _nextButton.onClick.AddListener(() => _next = true);
            _skipButton.onClick.AddListener(() => _skip = true);
            _deploy.SelectionChanged += _ => _selected = true;
            _deploy.Deployed += _ => _deployed = true;
            _rootRect = (RectTransform)_root.transform;
            Transform line = _highlight.Find("Border");   // the gold frame line pulses; the soft glow under it stays faint
            _highlightImage = line != null ? line.GetComponent<Image>() : null;
            _root.SetActive(false);
            ResetGates();
        }

        private void OnDestroy() => ResetGates();

        /// <summary>Runs the tutorial and waits until it ends (finished or skipped).</summary>
        public IEnumerator Run(bool replay)
        {
            Begin(replay);
            while (IsRunning) yield return null;
        }

        /// <summary>Starts the tutorial without waiting (Pause ▸ HOW TO PLAY).</summary>
        public void Begin(bool replay)
        {
            Stop();
            IsRunning = true;
            AllowDeploy = AllowEndTurn = false;
            _routine = StartCoroutine(Steps(replay));
        }

        /// <summary>Ends the tutorial at once without marking it done (restart, leaving the level).</summary>
        public void Stop()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            _root.SetActive(false);
            ResetGates();
            RefreshHud();
        }

        private static void ResetGates()
        {
            IsRunning = false;
            AllowDeploy = AllowEndTurn = true;
        }

        private IEnumerator Steps(bool replay)
        {
            GameManager gm = GameManager.Instance;
            _cam = Camera.main;
#if UNITY_EDITOR
            if (DevSkip) { Finish(gm); yield break; }
#endif
            _skip = false;
            _root.SetActive(true);
            RefreshHud();
            LevelData level = gm.Level;
            string place = level.locationName;
            int waves = level.waves.Length;

            // 1. Welcome
            yield return Say(1, "WELCOME, DEFENDER",
                $"Climate threats are coming down {place}. Stop them before they heat the planet. This short guide shows you how to play.",
                null, true);
            if (_skip) { Finish(gm); yield break; }

            // 2. Temperature
            yield return Say(2, "GLOBAL TEMPERATURE",
                $"This bar is the planet's temperature. Keep it below {level.lossThreshold:0.0} °C. An enemy that hits an empty lane adds +{level.heatPerUndefendedHit:0.00} °C.",
                () => UiRect(_temperatureTarget), true);
            if (_skip) { Finish(gm); yield break; }

            // 3. The road
            yield return Say(3, "THE ROAD",
                "Enemies stand on the far side of the road. There are 6 lanes, one enemy per lane, and each one fights the unit across from it.",
                EnemySlotsRect, true);
            if (_skip) { Finish(gm); yield break; }

            // 4. Oxygen
            yield return Say(4, "OXYGEN",
                "Units cost Oxygen. You get new Oxygen every turn, and unspent Oxygen is lost, so use it.",
                () => UiRect(_oxygenTarget), true);
            if (_skip) { Finish(gm); yield break; }

            // 5. Pick a unit (any card tap counts)
            _selected = false;
            yield return Say(5, "PICK A UNIT",
                "Tap a unit card. The number is its Oxygen cost. The line beside the cards shows its health and damage.",
                () => UiRect(_cardsTarget), false, () => _selected);
            if (_skip) { Finish(gm); yield break; }

            // 6. Deploy (skipped on a replay when nothing can be placed)
            int lane = DeployLane();
            bool canDeploy = lane >= 0 && _deploy.Selected != null && _oxygen.CanAfford(_deploy.Selected.opCost);
            if (canDeploy)
            {
                _deployed = false;
                AllowDeploy = true;
                yield return Say(6, "DEPLOY",
                    "Now tap the glowing slot on your side of the road. Put defenders in lanes that face an enemy.",
                    () => SlotRect(lane), false, () => _deployed);
                if (_skip) { Finish(gm); yield break; }
            }
            AllowDeploy = true;

            // 7. Tip
            yield return Say(7, "GOOD TO KNOW",
                $"Fill the lanes that face enemies first. An ally that attacks an empty enemy slot cools the planet by {level.coolingPerEmptyLaneAttack:0.00} °C, once per turn.",
                null, true);
            if (_skip) { Finish(gm); yield break; }

            // 8. End Turn: the lanes fight one by one, the coach box steps aside.
            AllowEndTurn = true;
            RefreshHud();
            yield return Say(8, "END TURN",
                "When you are ready, tap END TURN. Each lane fights in order: your unit attacks first, then the enemy hits back.",
                () => UiRect(_endTurnTarget), false, () => _turns.IsResolving);
            if (_skip) { Finish(gm); yield break; }
            ShowBox(false);
            while ((_turns.IsResolving || PopupManager.IsOpen) && !_skip) yield return null;
            if (_skip || gm.Current == GameManager.State.Ended) { Finish(gm); yield break; }

            // 9. Done
            yield return Say(9, "YOU'RE READY",
                $"Clear all {waves} waves to win. The lower the temperature stays, the more stars you earn. Pause ▸ HOW TO PLAY shows this again.",
                null, true, null, "START PLAYING");
            Finish(gm);
        }

        /// <summary>Shows one step and waits for NEXT (or for <paramref name="until"/> on action steps), or SKIP.</summary>
        private IEnumerator Say(int step, string title, string body, Func<Rect?> target, bool nextButton,
                                Func<bool> until = null, string nextLabel = "NEXT")
        {
            _stepLabel.text = $"STEP {step} / {StepCount}";
            _title.text = title;
            _body.text = body;
            _nextButton.gameObject.SetActive(nextButton);
            _nextLabel.text = nextLabel;
            _next = false;
            ShowBox(true);
            while (!_skip && (nextButton ? !_next : !(until?.Invoke() ?? true)))
            {
                // Follow the target every frame (the camera can still be settling, the screen can be resized).
                Rect? r = target?.Invoke();
                PlaceHighlight(r);
                PlaceCoach(r);
                yield return null;
            }
            PlaceHighlight(null);
        }

#if UNITY_EDITOR
        /// <summary>Editor play-tests: presses SKIP TUTORIAL.</summary>
        public void DevSkipNow() => _skip = true;
#endif

        private void Finish(GameManager gm)
        {
            if (gm != null) SettingsStore.SetTutorialDone(gm.PlayerName, true);
            _root.SetActive(false);
            _routine = null;
            ResetGates();
            RefreshHud();
        }

        private void RefreshHud()
        {
            if (_ui != null && GameManager.Instance != null && GameManager.Instance.Level != null) _ui.RefreshHud();
        }

        private void ShowBox(bool show)
        {
            _coach.gameObject.SetActive(show);
            if (!show) PlaceHighlight(null);
        }

        // ---------------------------------------------------------------- targets (in the tutorial root's local space)

        private Rect UiRect(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);   // screen pixels on an overlay canvas
            Vector2 a = ToLocal(corners[0]), b = ToLocal(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private Rect? EnemySlotsRect()
        {
            if (_cam == null) return null;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            for (int l = 0; l < IsoBoardLayout.LaneCount; l++)
            {
                Vector3 p = IsoBoardLayout.SlotPosition(l, true);
                foreach (Vector3 corner in new[] { p + new Vector3(-1f, -0.4f), p + new Vector3(1f, 1.7f) })
                {
                    Vector2 lp = ToLocal(_cam.WorldToScreenPoint(corner));
                    min = Vector2.Min(min, lp); max = Vector2.Max(max, lp);
                }
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private Rect? SlotRect(int lane)
        {
            if (_cam == null) return null;
            Vector3 p = IsoBoardLayout.SlotPosition(lane, false);
            Vector2 a = ToLocal(_cam.WorldToScreenPoint(p + new Vector3(-1.1f, -0.6f)));
            Vector2 b = ToLocal(_cam.WorldToScreenPoint(p + new Vector3(1.1f, 0.6f)));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private Vector2 ToLocal(Vector3 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootRect, screen, null, out Vector2 local);
            return local;
        }

        /// <summary>The free ally slot facing the first enemy; any free slot when no enemy lane is free; -1 if none.</summary>
        private int DeployLane()
        {
            for (int l = 0; l < _grid.LaneCount; l++)
                if (_grid.IsAllySlotFree(l) && _grid.GetEnemy(l) != null) return l;
            for (int l = 0; l < _grid.LaneCount; l++)
                if (_grid.IsAllySlotFree(l)) return l;
            return -1;
        }

        // ---------------------------------------------------------------- placing the frame and the coach box

        private void PlaceHighlight(Rect? r)
        {
            if (r == null) { _highlight.gameObject.SetActive(false); return; }
            Rect rect = r.Value;
            _highlight.gameObject.SetActive(true);
            _highlight.anchorMin = _highlight.anchorMax = new Vector2(0.5f, 0.5f);
            _highlight.pivot = new Vector2(0.5f, 0.5f);
            _highlight.anchoredPosition = rect.center + LocalOriginOffset();
            _highlight.sizeDelta = rect.size + new Vector2(16f, 16f);
            // Gentle pulse, also while the game is paused.
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            _highlight.localScale = Vector3.one * (1f + 0.035f * pulse);
            if (_highlightImage != null) { Color c = _highlightImage.color; c.a = 0.6f + 0.4f * pulse; _highlightImage.color = c; }
        }

        /// <summary>Local points are relative to the root's pivot; anchored positions at (0.5, 0.5) are relative to its centre.</summary>
        private Vector2 LocalOriginOffset()
        {
            Rect rr = _rootRect.rect;
            return -(rr.center);
        }

        /// <summary>
        /// Bottom-left above the footer, or top-left under the header when the target is in the lower part of the
        /// screen or would be covered by the box in its bottom place.
        /// </summary>
        private void PlaceCoach(Rect? target)
        {
            Rect rr = _rootRect.rect;
            var bottomBox = new Rect(rr.xMin + CoachMargin, rr.yMin + FooterHeight + 14f, _coach.sizeDelta.x, _coach.sizeDelta.y);
            bool top = target != null && (target.Value.center.y < rr.center.y - 40f || target.Value.Overlaps(bottomBox));
            if (top)
            {
                _coach.anchorMin = _coach.anchorMax = _coach.pivot = new Vector2(0f, 1f);
                _coach.anchoredPosition = new Vector2(CoachMargin, -(HeaderHeight + 14f));
            }
            else
            {
                _coach.anchorMin = _coach.anchorMax = _coach.pivot = new Vector2(0f, 0f);
                _coach.anchoredPosition = new Vector2(CoachMargin, FooterHeight + 14f);
            }
        }
    }
}
