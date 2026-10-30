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
        [Tooltip("Red ring on lanes the boss will hit next.")]
        [SerializeField] private SpriteRenderer _danger;

        private bool _deployable;
        private bool _selected;
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

        /// <summary>Shows a gold ring under the ally the player has picked up to move, swap or take back.</summary>
        public void SetSelected(bool selected)
        {
            _selected = selected && !_isEnemySide;
            RefreshVisual();
        }

        /// <summary>Shows the red warning ring (lanes the boss is about to hit).</summary>
        public void SetDanger(bool danger)
        {
            if (_danger != null) _danger.enabled = danger;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isEnemySide || DeploySystem.Instance == null) return;
            DeploySystem.Instance.OnAllySlotTapped(_lane);
        }

        public void OnPointerEnter(PointerEventData eventData) { _hovered = true; RefreshVisual(); }
        public void OnPointerExit(PointerEventData eventData) { _hovered = false; RefreshVisual(); }

        private void RefreshVisual()
        {
            if (_highlight == null) return;
            _highlight.enabled = _deployable || _selected;
            // High-contrast setting: solid yellow rings instead of the soft green. A picked-up unit's slot is gold.
            Color c = _selected ? new Color(0.886f, 0.682f, 0.173f)
                    : SettingsStore.HighContrast ? new Color(1f, 0.92f, 0.2f) : Color.white;
            c.a = _selected || _hovered ? 1f : (SettingsStore.HighContrast ? 0.95f : 0.65f);
            _highlight.color = c;
        }
    }
}
