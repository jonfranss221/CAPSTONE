using System.Collections;
using UnityEngine;

namespace ThermoTactics
{
    /// <summary>
    /// Fades a screen or dialog in when it opens, with a small upward slide of its content.
    /// Put it on the panel root; the content to slide is optional.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelFx : MonoBehaviour
    {
        [SerializeField] private RectTransform _slide;
        [SerializeField] private float _duration = 0.22f;
        [SerializeField] private float _slideDistance = 18f;

        private CanvasGroup _group;
        private Vector2 _slideHome;
        private bool _homeKnown;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_slide != null) { _slideHome = _slide.anchoredPosition; _homeKnown = true; }
        }

        private void OnEnable()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            StopAllCoroutines();
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            _group.alpha = 0f;
            for (float t = 0f; t < _duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / _duration);
                _group.alpha = k;
                if (_homeKnown) _slide.anchoredPosition = _slideHome + Vector2.down * (_slideDistance * (1f - k));
                yield return null;
            }
            _group.alpha = 1f;
            if (_homeKnown) _slide.anchoredPosition = _slideHome;
        }
    }
}
