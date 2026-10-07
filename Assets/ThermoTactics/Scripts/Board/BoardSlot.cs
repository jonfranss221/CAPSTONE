using UnityEngine;
using UnityEngine.EventSystems;

namespace ThermoTactics
{
    /// <summary>
    /// One of the 12 painted slots on the road. Ally slots can be tapped to deploy.
    /// Clicks arrive through the EventSystem (Physics2DRaycaster on the camera), so they work
    /// with mouse and touch, and are automatically blocked when a UI panel is in front.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BoardSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private int _lane;
        [SerializeField] private bool _isEnemySide;
        [SerializeField] private SpriteRenderer _highlight;

        private bool _deployable;
        private bool _hovered;

        public int Lane => _lane;
        public bool IsEnemySide => _isEnemySide;

        public void Configure(int lane, bool isEnemySide, SpriteRenderer highlight)
        {
            _lane = lane;
            _isEnemySide = isEnemySide;
            _highlight = highlight;
            RefreshVisual();
        }

        /// <summary>Shows the green deploy ring when the player may place a unit here.</summary>
        public void SetDeployable(bool deployable)
        {
            _deployable = deployable && !_isEnemySide;
            RefreshVisual();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isEnemySide || DeploySystem.Instance == null) return;
            DeploySystem.Instance.TryDeploy(_lane);
        }

        public void OnPointerEnter(PointerEventData eventData) { _hovered = true; RefreshVisual(); }
        public void OnPointerExit(PointerEventData eventData) { _hovered = false; RefreshVisual(); }

        private void RefreshVisual()
        {
            if (_highlight == null) return;
            _highlight.enabled = _deployable;
            Color c = _highlight.color;
            c.a = _hovered ? 1f : 0.65f;
            _highlight.color = c;
        }
    }
}
