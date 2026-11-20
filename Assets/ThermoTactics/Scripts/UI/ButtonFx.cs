using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>
    /// Button feel: grows a little on hover, squashes when pressed, springs back on release.
    /// Scales around the button's centre (the pivot is moved to the centre without moving the button).
    /// Does nothing while the button is disabled. Also plays the click sound: a "confirm" sound for the buttons that
    /// start or commit something (Play, Start, End Turn, Buy ...), the ordinary click for the rest.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private float _hoverScale = 1.035f;
        [SerializeField] private float _pressScale = 0.95f;
        [SerializeField] private float _speed = 18f;

        private RectTransform _rt;
        private Selectable _selectable;
        private bool _hover, _down;
        private float _scale = 1f;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _selectable = GetComponent<Selectable>();
            CentrePivot(_rt);
        }

        private void OnDisable()
        {
            _hover = _down = false;
            _scale = 1f;
            if (_rt != null) _rt.localScale = Vector3.one;
        }

        private static void CentrePivot(RectTransform rt)
        {
            Vector2 target = new Vector2(0.5f, 0.5f);
            Vector2 delta = target - rt.pivot;
            if (delta == Vector2.zero) return;
            Vector2 size = rt.rect.size;
            rt.pivot = target;
            rt.anchoredPosition += new Vector2(delta.x * size.x, delta.y * size.y);
        }

        public void OnPointerEnter(PointerEventData e) => _hover = true;
        public void OnPointerExit(PointerEventData e) { _hover = false; _down = false; }
        public void OnPointerDown(PointerEventData e) => _down = true;
        public void OnPointerUp(PointerEventData e) => _down = false;

        private static readonly string[] ConfirmButtons =
            { "PlayButton", "StartButton", "EndTurnButton", "BuyButton", "PrimaryButton", "ContinueButton", "ResumeButton" };

        public void OnPointerClick(PointerEventData e)
        {
            if (_selectable != null && !_selectable.IsInteractable()) return;
            AudioManager.Play(System.Array.IndexOf(ConfirmButtons, gameObject.name) >= 0 ? Sfx.Confirm : Sfx.Click);
        }

        private void Update()
        {
            bool active = _selectable == null || _selectable.IsInteractable();
            float target = !active ? 1f : _down ? _pressScale : _hover ? _hoverScale : 1f;
            _scale = Mathf.Lerp(_scale, target, 1f - Mathf.Exp(-_speed * Time.unscaledDeltaTime));
            _rt.localScale = new Vector3(_scale, _scale, 1f);
        }
    }
}
