using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>One stop on the level path: a numbered square (diamond for the elite spike, large for the boss).</summary>
    public class LevelNode : MonoBehaviour
    {
        public enum NodeState { Cleared, Current, Locked }

        [SerializeField] private int _levelNumber = 1;
        [SerializeField] private Button _button;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _border;
        [SerializeField] private Text _label;
        [SerializeField] private Text _caption;
        [SerializeField] private GameObject _selectRing;
        [SerializeField] private bool _isSpike;
        [SerializeField] private bool _isBoss;

        public int LevelNumber => _levelNumber;
        public Button Button => _button;

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }

        public void Set(NodeState state, bool selected, string caption)
        {
            Color bg = Hex("#1A2620"), fg = Hex("#9F978A"), border = Hex("#4A5A4E");
            if (state == NodeState.Cleared) { bg = Hex("#E2AE2C"); fg = Hex("#1A140A"); border = Hex("#1A140A"); }
            if (state == NodeState.Current) { bg = Hex("#A3241C"); fg = Hex("#FFF6E6"); border = Hex("#1A140A"); }
            if (state == NodeState.Locked && _isSpike) { border = Hex("#A3241C"); fg = Hex("#E7A89F"); }
            if (state == NodeState.Locked && _isBoss) { border = Hex("#A3241C"); fg = Hex("#E7A89F"); bg = Hex("#24130F"); }
            _fill.color = bg;
            _border.color = border;
            _label.color = fg;
            _label.text = _isBoss ? "BOSS" : _levelNumber.ToString();
            _caption.text = caption;
            _caption.color = state == NodeState.Locked ? Hex("#9F978A") : Hex("#F3EAD7");
            _selectRing.SetActive(selected);
        }
    }
}
