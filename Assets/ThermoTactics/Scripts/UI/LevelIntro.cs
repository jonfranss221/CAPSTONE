using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// The opening of every level: the screen fades in from black while the camera glides to the full board,
    /// and a title card shows where the player is ("STAGE 1 · BAGUIO FOREST / LEVEL 2 / LION'S HEAD").
    /// The card holds about 2 seconds; a tap anywhere skips it. The player cannot act until it is gone.
    /// Uses unscaled time, so the Battle speed setting does not change it.
    /// </summary>
    public class LevelIntro : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _fade;
        [SerializeField] private CanvasGroup _card;
        [SerializeField] private Text _eyebrow;
        [SerializeField] private Text _level;
        [SerializeField] private Text _place;
        [SerializeField] private Text _subtitle;
        [SerializeField] private Text _tag;
        [SerializeField] private Button _skipButton;

        [Header("Timing (seconds)")]
        [SerializeField] private float _fadeTime = 0.6f;
        [SerializeField] private float _cardInTime = 0.35f;
        [SerializeField] private float _holdTime = 2.2f;
        [SerializeField] private float _cardOutTime = 0.35f;
        [SerializeField] private float _glideTime = 1.6f;

        /// <summary>True while the intro is on screen (GameManager.CanPlayerAct waits for it).</summary>
        public static bool IsPlaying { get; private set; }

#if UNITY_EDITOR
        /// <summary>Editor play-tests: the intro returns at once.</summary>
        public static bool DevSkip;
#endif

        private bool _skip;
        private Coroutine _routine;
        private RectTransform _cardRect;
        private Vector2 _cardHome;

        private void Awake()
        {
            _skipButton.onClick.AddListener(() => _skip = true);
            _cardRect = (RectTransform)_card.transform;
            _cardHome = _cardRect.anchoredPosition;
            _root.SetActive(false);
            IsPlaying = false;
        }

        private void OnDestroy() => IsPlaying = false;

        /// <summary>
        /// Plays the intro and waits until it is done. <paramref name="resumeTag"/> replaces the waves line
        /// when a saved game is continued ("WELCOME BACK · WAVE 2 · TURN 3").
        /// </summary>
        public IEnumerator Play(StageData stage, LevelData level, int levelNumber, string resumeTag, BattleCamera cam)
        {
            Stop();
            IsPlaying = true;
            _routine = StartCoroutine(Run(stage, level, levelNumber, resumeTag, cam));
            while (IsPlaying) yield return null;
        }

        /// <summary>Ends the intro at once (restart, leaving the scene).</summary>
        public void Stop()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            _root.SetActive(false);
            IsPlaying = false;
        }

        private IEnumerator Run(StageData stage, LevelData level, int levelNumber, string resumeTag, BattleCamera cam)
        {
#if UNITY_EDITOR
            if (DevSkip)
            {
                if (cam != null) cam.SnapHome();
                Finish();
                yield break;
            }
#endif
            Fill(stage, level, levelNumber, resumeTag);
            _skip = false;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            SetFade(1f);
            _card.alpha = 0f;

            if (cam != null) cam.StartCoroutine(cam.IntroGlide(_glideTime));

            // 1. Fade in from black.
            for (float t = 0f; t < _fadeTime; t += Time.unscaledDeltaTime)
            {
                SetFade(1f - Mathf.SmoothStep(0f, 1f, t / _fadeTime));
                yield return null;
            }
            SetFade(0f);

            // 2. The title card slides up and fades in.
            for (float t = 0f; t < _cardInTime; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / _cardInTime);
                _card.alpha = k;
                _cardRect.anchoredPosition = _cardHome + Vector2.down * (24f * (1f - k));
                yield return null;
            }
            _card.alpha = 1f;
            _cardRect.anchoredPosition = _cardHome;

            // 3. Hold (a tap skips).
            for (float t = 0f; t < _holdTime && !_skip; t += Time.unscaledDeltaTime) yield return null;

            // 4. Fade out.
            for (float t = 0f; t < _cardOutTime; t += Time.unscaledDeltaTime)
            {
                _card.alpha = 1f - t / _cardOutTime;
                yield return null;
            }
            if (cam != null && !cam.IsHome) cam.SnapHome();
            Finish();
        }

        private void Finish()
        {
            _root.SetActive(false);
            _routine = null;
            IsPlaying = false;
        }

        private void Fill(StageData stage, LevelData level, int levelNumber, string resumeTag)
        {
            _eyebrow.text = $"STAGE {stage.stageId} · {stage.stageName.ToUpperInvariant()}";
            _level.text = $"LEVEL {levelNumber}";
            _place.text = level.locationName.ToUpperInvariant();
            _subtitle.text = level.subtitle;
            if (!string.IsNullOrEmpty(resumeTag))
            {
                _tag.text = resumeTag;
                return;
            }
            string tag = $"{level.waves.Length} WAVES · KEEP BELOW {level.lossThreshold:0.0} °C";
            if (level.isBossLevel) tag = "BOSS LEVEL · " + tag;
            else if (level.isEliteSpike) tag = "ELITE INVASION · " + tag;
            _tag.text = tag;
        }

        private void SetFade(float a)
        {
            Color c = _fade.color; c.a = a; _fade.color = c;
        }
    }
}
